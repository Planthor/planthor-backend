using System.Net;
using System.Net.Http.Json;
using Adapters.Strava.Configuration;
using Adapters.Strava.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Adapters.Strava.Client;

/// <summary>
/// Handles Strava OAuth token operations including exchanging authorization codes, 
/// refreshing tokens proactively, and deauthorizing users.
/// </summary>
/// <param name="httpClient">The HTTP client used to communicate with the Strava token endpoint.</param>
/// <param name="tokenDb">The database repository for managing Strava tokens.</param>
/// <param name="options">The configuration options for Strava.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="clock">The system clock used for token expiration checks.</param>
internal sealed partial class StravaTokenClient(
    HttpClient httpClient,
    StravaAdapterDatabase tokenDb,
    IOptions<StravaOptions> options,
    ILogger<StravaTokenClient> logger,
    IClock clock)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly StravaAdapterDatabase _tokenDb = tokenDb ?? throw new ArgumentNullException(nameof(tokenDb));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly StravaOptions _options = options.Value;

    private const int TokenRefreshThresholdSeconds = 60;

    private Uri TokenEndpoint => new(_options.BaseUrl, "oauth/token");
    private Uri DeauthorizeEndpoint => new(_options.BaseUrl, "oauth/deauthorize");

    /// <summary>
    /// Exchanges an authorization code and persists the resulting athlete tokens.
    /// </summary>
    /// <param name="code">The authorization code returned by Strava.</param>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The exchanged token response, or <c>null</c> if the exchange failed.</returns>
    internal async Task<StravaTokenResponse?> ExchangeCodeAsync(
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
        using var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
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

        await _tokenDb.UpsertAsync(new StravaTokenDocument
        {
            Id = identifyName,
            AthleteId = tokenResponse.Athlete.Id,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = tokenResponse.ExpiresAt,
            LastRefreshedAtUtc = _clock.GetCurrentInstant().ToDateTimeOffset()
        }, cancellationToken);

        LogTokenExchangeSucceeded(identifyName, tokenResponse.Athlete.Id);
        return tokenResponse;
    }

    /// <summary>
    /// Refreshes and immediately persists a rotated Strava token pair.
    /// </summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The newly refreshed and persisted token document, or <c>null</c> if the refresh failed.</returns>
    internal async Task<StravaTokenDocument?> RefreshTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        var existing = await _tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
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

        using var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
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
        existing.LastRefreshedAtUtc = _clock.GetCurrentInstant().ToDateTimeOffset();
        await _tokenDb.UpsertAsync(existing, cancellationToken);

        LogTokenRefreshSucceeded(identifyName);
        return existing;
    }

    /// <summary>
    /// Gets a usable access token, refreshing proactively when required.
    /// </summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A valid token document, or <c>null</c> if no valid token could be obtained.</returns>
    internal async Task<StravaTokenDocument?> GetValidTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        var token = await _tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (token is null)
        {
            return null;
        }

        return _clock.GetCurrentInstant().ToUnixTimeSeconds() > token.ExpiresAt - TokenRefreshThresholdSeconds
            ? await RefreshTokenAsync(identifyName, cancellationToken)
            : token;
    }

    /// <summary>
    /// Deauthorizes Strava and permanently removes the local token document.
    /// </summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><c>true</c> if deauthorization was successful; otherwise, <c>false</c>.</returns>
    internal async Task<bool> DeauthorizeAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        var token = await _tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
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

            using var response = await _httpClient.PostAsync(DeauthorizeEndpoint, content, cancellationToken);
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
            await _tokenDb.DeleteAsync(identifyName, cancellationToken);
        }

        return true;
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
}
