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
/// Typed Strava HTTP client for fetching activity data. Handles automatic token injection, one-time 401 refresh, and typed failures.
/// Token operations (exchange, refresh, and deauthorization) are delegated to <see cref="StravaTokenClient"/>.
/// </summary>
/// <param name="httpClient">The HTTP client used to communicate with the Strava API.</param>
/// <param name="tokenClient">The client for managing and refreshing tokens.</param>
/// <param name="options">The configuration options for Strava.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="clock">The system clock.</param>
/// <param name="rateLimitCoordinator">The rate limit coordinator.</param>
internal sealed partial class StravaApiClient(
    HttpClient httpClient,
    StravaTokenClient tokenClient,
    IOptions<StravaOptions> options,
    ILogger<StravaApiClient> logger,
    IClock clock,
    StravaRateLimitCoordinator rateLimitCoordinator) : IStravaApiClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly StravaTokenClient _tokenClient = tokenClient ?? throw new ArgumentNullException(nameof(tokenClient));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly StravaOptions _options = options.Value;

    /// <inheritdoc />
    public Task<StravaTokenResponse?> ExchangeCodeAsync(
        string code,
        string identifyName,
        CancellationToken cancellationToken) =>
        _tokenClient.ExchangeCodeAsync(code, identifyName, cancellationToken);

    /// <inheritdoc />
    public Task<StravaTokenDocument?> RefreshTokenAsync(
        string identifyName,
        CancellationToken cancellationToken) =>
        _tokenClient.RefreshTokenAsync(identifyName, cancellationToken);

    /// <inheritdoc />
    public Task<StravaTokenDocument?> GetValidTokenAsync(
        string identifyName,
        CancellationToken cancellationToken) =>
        _tokenClient.GetValidTokenAsync(identifyName, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeauthorizeAsync(
        string identifyName,
        CancellationToken cancellationToken) =>
        _tokenClient.DeauthorizeAsync(identifyName, cancellationToken);

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

        var relativeUriString = string.Create(
            CultureInfo.InvariantCulture,
            $"api/v3/athlete/activities?after={afterEpoch}&before={beforeEpoch}&page={page}&per_page={perPage}");
        var relativeUri = new Uri(relativeUriString, UriKind.Relative);

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

        var token = await _tokenClient.GetValidTokenAsync(identifyName, cancellationToken);
        if (token is null)
        {
            return new StravaApiResult<StravaActivityResponse>(
                StravaApiOutcome.AuthorizationRequired,
                ErrorCode: "strava_token_missing");
        }

        var relativeUriString = $"api/v3/activities/{Uri.EscapeDataString(externalActivityId)}";
        var relativeUri = new Uri(relativeUriString, UriKind.Relative);

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
            var token = await _tokenClient.GetValidTokenAsync(identifyName, cancellationToken);
            if (token is null)
            {
                return new StravaApiResult<T>(
                    StravaApiOutcome.AuthorizationRequired,
                    ErrorCode: "strava_authorization_required");
            }

            using var firstRequest = requestFactory(token.AccessToken);
            using var firstResponse = await _httpClient.SendAsync(firstRequest, cancellationToken);
            rateLimitCoordinator.Observe(firstResponse);
            if (firstResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                return await ReadActivityResponseAsync<T>(firstResponse, cancellationToken);
            }

            var refreshed = await _tokenClient.RefreshTokenAsync(identifyName, cancellationToken);
            if (refreshed is null)
            {
                return new StravaApiResult<T>(
                    StravaApiOutcome.AuthorizationRequired,
                    ErrorCode: "strava_token_refresh_failed");
            }

            using var retryRequest = requestFactory(refreshed.AccessToken);
            using var retryResponse = await _httpClient.SendAsync(retryRequest, cancellationToken);
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
        RetryAt: _clock.GetCurrentInstant().Plus(Duration.FromMinutes(1)),
        ErrorCode: "strava_temporarily_unavailable");

    private HttpRequestMessage CreateAuthorizedGet(Uri relativeUri, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.BaseUrl, relativeUri));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava activity request failed for member {IdentifyName}")]
    private partial void LogActivityRequestFailed(Exception exception, string identifyName);
}
