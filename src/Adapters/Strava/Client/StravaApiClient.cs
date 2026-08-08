using System.Net.Http.Json;
using Adapters.Strava.Configuration;
using Adapters.Strava.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adapters.Strava.Client;

/// <summary>
/// Typed HTTP client for the Strava API (<c>https://www.strava.com/api/v3</c>).
/// Handles OAuth token exchange, token refresh with rotation,
/// and deauthorization.
/// </summary>
/// <remarks>
/// Registered via <c>AddHttpClient&lt;StravaApiClient&gt;</c> in DI.
/// The underlying <see cref="HttpClient"/> is managed by the <c>IHttpClientFactory</c>
/// infrastructure and should not be disposed manually.
/// </remarks>
public partial class StravaApiClient : IStravaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly StravaAdapterDatabase _tokenDb;
    private readonly StravaOptions _options;
    private readonly ILogger<StravaApiClient> _logger;

    public StravaApiClient(
        HttpClient httpClient,
        StravaAdapterDatabase tokenDb,
        IOptions<StravaOptions> options,
        ILogger<StravaApiClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(tokenDb);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _tokenDb = tokenDb;
        _options = options.Value;
        _logger = logger;
    }

    private string TokenEndpoint => $"{_options.BaseUrl.TrimEnd('/')}/oauth/token";
    private string DeauthorizeEndpoint => $"{_options.BaseUrl.TrimEnd('/')}/oauth/deauthorize";

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens,
    /// and persists them in the adapter database.
    /// </summary>
    /// <param name="code">The authorization code received from Strava's OAuth callback.</param>
    /// <param name="identifyName">The Planthor member identifier to associate with the tokens.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// A <see cref="StravaTokenResponse"/> containing the tokens and athlete information,
    /// or <c>null</c> if the exchange failed.
    /// </returns>
    public async Task<StravaTokenResponse?> ExchangeCodeAsync(
        string code,
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(identifyName);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code"
        });

        var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogTokenExchangeFailed(response.StatusCode);
            return null;
        }

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<StravaTokenResponse>(cancellationToken);

        if (tokenResponse is null)
        {
            LogTokenResponseDeserializationFailed();
            return null;
        }

        var document = new StravaTokenDocument
        {
            Id = identifyName,
            AthleteId = tokenResponse.Athlete.Id,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = tokenResponse.ExpiresAt,
            LastRefreshedAtUtc = DateTime.UtcNow
        };

        await _tokenDb.UpsertAsync(document, cancellationToken);

        LogTokenExchangeSucceeded(identifyName, tokenResponse.Athlete.Id);
        return tokenResponse;
    }

    /// <summary>
    /// Refreshes an expired access token using the stored refresh token.
    /// </summary>
    /// <param name="identifyName">The Planthor member identifier whose token should be refreshed.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// The updated <see cref="StravaTokenDocument"/>, or <c>null</c> if refresh failed
    /// (e.g., the user has deauthorized the application).
    /// </returns>
    /// <remarks>
    /// Strava may rotate the refresh token on every response. This method
    /// always persists the latest <c>refresh_token</c> from the response.
    /// </remarks>
    public async Task<StravaTokenDocument?> RefreshTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        var existing = await _tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (existing is null)
        {
            LogNoTokenFound(identifyName);
            return null;
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = existing.RefreshToken,
            ["grant_type"] = "refresh_token"
        });

        var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogTokenRefreshFailed(identifyName, response.StatusCode);
            return null;
        }

        var refreshResponse = await response.Content
            .ReadFromJsonAsync<StravaRefreshResponse>(cancellationToken);

        if (refreshResponse is null)
        {
            LogTokenResponseDeserializationFailed();
            return null;
        }

        // CRITICAL: Persist the new refresh token immediately.
        existing.AccessToken = refreshResponse.AccessToken;
        existing.RefreshToken = refreshResponse.RefreshToken;
        existing.ExpiresAt = refreshResponse.ExpiresAt;
        existing.LastRefreshedAtUtc = DateTime.UtcNow;

        await _tokenDb.UpsertAsync(existing, cancellationToken);

        LogTokenRefreshSucceeded(identifyName);
        return existing;
    }

    /// <summary>
    /// Retrieves a valid access token for the specified member,
    /// refreshing proactively if the token is near expiry.
    /// </summary>
    /// <param name="identifyName">The Planthor member identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// The current <see cref="StravaTokenDocument"/> with a valid access token,
    /// or <c>null</c> if no token exists or refresh failed.
    /// </returns>
    public async Task<StravaTokenDocument?> GetValidTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        var token = await _tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (token is null)
        {
            return null;
        }

        // Proactive refresh: refresh if within 60 seconds of expiry
        var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (nowEpoch > token.ExpiresAt - 60)
        {
            return await RefreshTokenAsync(identifyName, cancellationToken);
        }

        return token;
    }

    /// <summary>
    /// Deauthorizes the application from the member's Strava account
    /// and removes the stored tokens.
    /// </summary>
    /// <param name="identifyName">The Planthor member identifier to deauthorize.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><c>true</c> if deauthorization succeeded or token was already removed; otherwise <c>false</c>.</returns>
    public async Task<bool> DeauthorizeAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        var token = await _tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (token is null)
        {
            // Already disconnected
            return true;
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["access_token"] = token.AccessToken
        });

        var response = await _httpClient.PostAsync(DeauthorizeEndpoint, content, cancellationToken);

        // Delete tokens regardless — if deauth failed, user may have already revoked on Strava
        await _tokenDb.DeleteAsync(identifyName, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogDeauthorizationFailed(identifyName, response.StatusCode);
        }
        else
        {
            LogDeauthorizationSucceeded(identifyName);
        }

        return true;
    }

    // ────────────────────────────────────────────────────────────────
    // High-performance structured logging
    // ────────────────────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava token exchange failed with status {StatusCode}")]
    private partial void LogTokenExchangeFailed(System.Net.HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize Strava token response")]
    private partial void LogTokenResponseDeserializationFailed();

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava token exchange succeeded for member {IdentifyName}, athlete {AthleteId}")]
    private partial void LogTokenExchangeSucceeded(string identifyName, long athleteId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No Strava token found for member {IdentifyName}")]
    private partial void LogNoTokenFound(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava token refresh failed for member {IdentifyName} with status {StatusCode}")]
    private partial void LogTokenRefreshFailed(string identifyName, System.Net.HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava token refresh succeeded for member {IdentifyName}")]
    private partial void LogTokenRefreshSucceeded(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava deauthorization failed for member {IdentifyName} with status {StatusCode}")]
    private partial void LogDeauthorizationFailed(string identifyName, System.Net.HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava deauthorization succeeded for member {IdentifyName}")]
    private partial void LogDeauthorizationSucceeded(string identifyName);
}
