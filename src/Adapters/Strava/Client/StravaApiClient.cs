using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Adapters.Strava.Configuration;
using Adapters.Strava.Coordinator;
using Adapters.Strava.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Adapters.Strava.Client;

/// <summary>
/// Typed Strava HTTP client with proactive token refresh, one-time 401 refresh, and typed failures.
/// </summary>
/// <param name="httpClient">The HTTP client used to communicate with the Strava API.</param>
/// <param name="tokenDb">The database repository for managing tokens.</param>
/// <param name="options">The configuration options for Strava.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="clock">The system clock.</param>
/// <param name="rateLimitCoordinator">The rate limit coordinator.</param>
public sealed partial class StravaApiClient(
    HttpClient httpClient,
    StravaAdapterDatabase tokenDb,
    IOptions<StravaOptions> options,
    ILogger<StravaApiClient> logger,
    IClock clock,
    StravaRateLimitCoordinator rateLimitCoordinator) : IStravaApiClient
{
    private readonly StravaOptions _options = options.Value;

    private Uri TokenEndpoint => new(_options.BaseUrl, "oauth/token");
    private Uri DeauthorizeEndpoint => new(_options.BaseUrl, "oauth/deauthorize");

    /// <inheritdoc />
    public async Task<StravaTokenResponse?> ExchangeCodeAsync(
        string code,
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code"
        });
        using var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            LogTokenExchangeFailed(response.StatusCode);
            return null;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<StravaTokenResponse>(cancellationToken);
        if (tokenResponse?.Athlete is null)
        {
            LogTokenResponseDeserializationFailed();
            return null;
        }

        await tokenDb.UpsertAsync(new StravaTokenDocument
        {
            Id = identifyName,
            AthleteId = tokenResponse.Athlete.Id,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = tokenResponse.ExpiresAt,
            LastRefreshedAtUtc = clock.GetCurrentInstant().ToDateTimeOffset()
        }, cancellationToken);

        LogTokenExchangeSucceeded(identifyName, tokenResponse.Athlete.Id);
        return tokenResponse;
    }

    /// <inheritdoc />
    public async Task<StravaTokenDocument?> RefreshTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        var existing = await tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (existing is null)
        {
            LogNoTokenFound(identifyName);
            return null;
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = existing.RefreshToken,
            ["grant_type"] = "refresh_token"
        });
        using var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            LogTokenRefreshFailed(identifyName, response.StatusCode);
            return null;
        }

        var refreshResponse = await response.Content.ReadFromJsonAsync<StravaRefreshResponse>(cancellationToken);
        if (refreshResponse is null)
        {
            LogTokenResponseDeserializationFailed();
            return null;
        }

        existing.AccessToken = refreshResponse.AccessToken;
        existing.RefreshToken = refreshResponse.RefreshToken;
        existing.ExpiresAt = refreshResponse.ExpiresAt;
        existing.LastRefreshedAtUtc = clock.GetCurrentInstant().ToDateTimeOffset();
        await tokenDb.UpsertAsync(existing, cancellationToken);

        LogTokenRefreshSucceeded(identifyName);
        return existing;
    }

    /// <inheritdoc />
    public async Task<StravaTokenDocument?> GetValidTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        var token = await tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (token is null)
        {
            return null;
        }

        return clock.GetCurrentInstant().ToUnixTimeSeconds() > token.ExpiresAt - 60
            ? await RefreshTokenAsync(identifyName, cancellationToken)
            : token;
    }

    /// <inheritdoc />
    public async Task<bool> DeauthorizeAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        var token = await tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (token is null)
        {
            return true;
        }

        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["access_token"] = token.AccessToken
            });
            using var response = await httpClient.PostAsync(DeauthorizeEndpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogDeauthorizationFailed(identifyName, response.StatusCode);
            }
            else
            {
                LogDeauthorizationSucceeded(identifyName);
            }
        }
        finally
        {
            await tokenDb.DeleteAsync(identifyName, cancellationToken);
        }

        return true;
    }

    /// <inheritdoc />
    public Task<StravaApiResult<IReadOnlyList<StravaActivityResponse>>> GetAthleteActivitiesPageAsync(
        string identifyName,
        long afterEpoch,
        long beforeEpoch,
        int page,
        int perPage,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);
        if (rateLimitCoordinator.GetHistoricalDeferral() is { } deferredUntil)
        {
            return Task.FromResult(new StravaApiResult<IReadOnlyList<StravaActivityResponse>>(
                StravaApiOutcome.RateLimited,
                RetryAt: deferredUntil,
                ErrorCode: "strava_rate_limit_headroom"));
        }

        var relativeUri = string.Create(
            CultureInfo.InvariantCulture,
            $"api/v3/athlete/activities?after={afterEpoch}&before={beforeEpoch}&page={page}&per_page={perPage}");
        return SendAuthorizedAsync<IReadOnlyList<StravaActivityResponse>>(
            identifyName,
            accessToken => CreateAuthorizedGet(relativeUri, accessToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StravaApiResult<StravaActivityResponse>> GetActivityAsync(
        string identifyName,
        string externalActivityId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);
        ArgumentException.ThrowIfNullOrEmpty(externalActivityId);

        var token = await tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (token is null)
        {
            return new StravaApiResult<StravaActivityResponse>(
                StravaApiOutcome.AuthorizationRequired,
                ErrorCode: "strava_token_missing");
        }

        var relativeUri = $"api/v3/activities/{Uri.EscapeDataString(externalActivityId)}";
        var result = await SendAuthorizedAsync<StravaActivityResponse>(
            identifyName,
            accessToken => CreateAuthorizedGet(relativeUri, accessToken),
            cancellationToken);

        if (result.Outcome == StravaApiOutcome.Success &&
            result.Value?.Athlete?.Id != token.AthleteId)
        {
            return new StravaApiResult<StravaActivityResponse>(
                StravaApiOutcome.AuthorizationRequired,
                ErrorCode: "strava_activity_owner_mismatch");
        }

        return result;
    }

    private async Task<StravaApiResult<T>> SendAuthorizedAsync<T>(
        string identifyName,
        Func<string, HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetValidTokenAsync(identifyName, cancellationToken);
            if (token is null)
            {
                return new StravaApiResult<T>(
                    StravaApiOutcome.AuthorizationRequired,
                    ErrorCode: "strava_authorization_required");
            }

            using var firstRequest = requestFactory(token.AccessToken);
            using var firstResponse = await httpClient.SendAsync(firstRequest, cancellationToken);
            rateLimitCoordinator.Observe(firstResponse);
            if (firstResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                return await ReadActivityResponseAsync<T>(firstResponse, cancellationToken);
            }

            var refreshed = await RefreshTokenAsync(identifyName, cancellationToken);
            if (refreshed is null)
            {
                return new StravaApiResult<T>(
                    StravaApiOutcome.AuthorizationRequired,
                    ErrorCode: "strava_token_refresh_failed");
            }

            using var retryRequest = requestFactory(refreshed.AccessToken);
            using var retryResponse = await httpClient.SendAsync(retryRequest, cancellationToken);
            rateLimitCoordinator.Observe(retryResponse);
            return await ReadActivityResponseAsync<T>(retryResponse, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogActivityRequestFailed(exception, identifyName);
            return TransientFailure<T>();
        }
        catch (JsonException exception)
        {
            LogActivityRequestFailed(exception, identifyName);
            return TransientFailure<T>();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogActivityRequestFailed(exception, identifyName);
            return TransientFailure<T>();
        }
    }

    private async Task<StravaApiResult<T>> ReadActivityResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null
                ? TransientFailure<T>()
                : new StravaApiResult<T>(StravaApiOutcome.Success, value);
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => new StravaApiResult<T>(
                StravaApiOutcome.NotFound,
                ErrorCode: "strava_activity_not_found"),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new StravaApiResult<T>(
                StravaApiOutcome.AuthorizationRequired,
                ErrorCode: "strava_authorization_required"),
            HttpStatusCode.TooManyRequests => new StravaApiResult<T>(
                StravaApiOutcome.RateLimited,
                RetryAt: rateLimitCoordinator.GetRetryAt(),
                ErrorCode: "strava_rate_limited"),
            _ => TransientFailure<T>()
        };
    }

    private StravaApiResult<T> TransientFailure<T>() => new(
        StravaApiOutcome.TransientFailure,
        RetryAt: clock.GetCurrentInstant().Plus(Duration.FromMinutes(1)),
        ErrorCode: "strava_temporarily_unavailable");

    private HttpRequestMessage CreateAuthorizedGet(string relativeUri, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.BaseUrl, relativeUri));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava token exchange failed with status {StatusCode}")]
    private partial void LogTokenExchangeFailed(HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize a Strava token response")]
    private partial void LogTokenResponseDeserializationFailed();

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava token exchange succeeded for member {IdentifyName}, athlete {AthleteId}")]
    private partial void LogTokenExchangeSucceeded(string identifyName, long athleteId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No Strava token found for member {IdentifyName}")]
    private partial void LogNoTokenFound(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava token refresh failed for member {IdentifyName} with status {StatusCode}")]
    private partial void LogTokenRefreshFailed(string identifyName, HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava token refresh succeeded for member {IdentifyName}")]
    private partial void LogTokenRefreshSucceeded(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava deauthorization failed for member {IdentifyName} with status {StatusCode}")]
    private partial void LogDeauthorizationFailed(string identifyName, HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava deauthorization succeeded for member {IdentifyName}")]
    private partial void LogDeauthorizationSucceeded(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava activity request failed for member {IdentifyName}")]
    private partial void LogActivityRequestFailed(Exception exception, string identifyName);
}
