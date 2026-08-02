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
public partial class StravaApiClient(
    HttpClient httpClient,
    StravaAdapterDatabase tokenDb,
    IOptions<StravaOptions> options,
    ILogger<StravaApiClient> logger) : IStravaApiClient
{
    private readonly StravaOptions _options = options.Value;

    private string TokenEndpoint => $"{_options.BaseUrl.TrimEnd('/')}/oauth/token";
    private string DeauthorizeEndpoint => $"{_options.BaseUrl.TrimEnd('/')}/oauth/deauthorize";

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens,
    /// and persists them in the adapter database.
    /// </summary>
    /// <param name="code">The authorization code received from Strava's OAuth callback.</param>
    /// <param name="memberId">The Planthor member identifier to associate with the tokens.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// A <see cref="StravaTokenResponse"/> containing the tokens and athlete information,
    /// or <c>null</c> if the exchange failed.
    /// </returns>
    public async Task<StravaTokenResponse?> ExchangeCodeAsync(
        string code,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code"
        });

        var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken);

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
            Id = memberId,
            AthleteId = tokenResponse.Athlete.Id,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = tokenResponse.ExpiresAt,
            LastRefreshedAtUtc = DateTime.UtcNow
        };

        await tokenDb.UpsertAsync(document, cancellationToken);

        LogTokenExchangeSucceeded(memberId, tokenResponse.Athlete.Id);
        return tokenResponse;
    }

    /// <summary>
    /// Refreshes an expired access token using the stored refresh token.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier whose token should be refreshed.</param>
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
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var existing = await tokenDb.GetByMemberIdAsync(memberId, cancellationToken);
        if (existing is null)
        {
            LogNoTokenFound(memberId);
            return null;
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = existing.RefreshToken,
            ["grant_type"] = "refresh_token"
        });

        var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogTokenRefreshFailed(memberId, response.StatusCode);
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

        await tokenDb.UpsertAsync(existing, cancellationToken);

        LogTokenRefreshSucceeded(memberId);
        return existing;
    }

    /// <summary>
    /// Retrieves a valid access token for the specified member,
    /// refreshing proactively if the token is near expiry.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// The current <see cref="StravaTokenDocument"/> with a valid access token,
    /// or <c>null</c> if no token exists or refresh failed.
    /// </returns>
    public async Task<StravaTokenDocument?> GetValidTokenAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var token = await tokenDb.GetByMemberIdAsync(memberId, cancellationToken);
        if (token is null)
        {
            return null;
        }

        // Proactive refresh: refresh if within 60 seconds of expiry
        var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (nowEpoch > token.ExpiresAt - 60)
        {
            return await RefreshTokenAsync(memberId, cancellationToken);
        }

        return token;
    }

    /// <summary>
    /// Deauthorizes the application from the member's Strava account
    /// and removes the stored tokens.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier to deauthorize.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><c>true</c> if deauthorization succeeded or token was already removed; otherwise <c>false</c>.</returns>
    public async Task<bool> DeauthorizeAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var token = await tokenDb.GetByMemberIdAsync(memberId, cancellationToken);
        if (token is null)
        {
            // Already disconnected
            return true;
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["access_token"] = token.AccessToken
        });

        var response = await httpClient.PostAsync(DeauthorizeEndpoint, content, cancellationToken);

        // Delete tokens regardless — if deauth failed, user may have already revoked on Strava
        await tokenDb.DeleteAsync(memberId, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogDeauthorizationFailed(memberId, response.StatusCode);
        }
        else
        {
            LogDeauthorizationSucceeded(memberId);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava token exchange succeeded for member {MemberId}, athlete {AthleteId}")]
    private partial void LogTokenExchangeSucceeded(Guid memberId, long athleteId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No Strava token found for member {MemberId}")]
    private partial void LogNoTokenFound(Guid memberId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava token refresh failed for member {MemberId} with status {StatusCode}")]
    private partial void LogTokenRefreshFailed(Guid memberId, System.Net.HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava token refresh succeeded for member {MemberId}")]
    private partial void LogTokenRefreshSucceeded(Guid memberId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava deauthorization failed for member {MemberId} with status {StatusCode}")]
    private partial void LogDeauthorizationFailed(Guid memberId, System.Net.HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava deauthorization succeeded for member {MemberId}")]
    private partial void LogDeauthorizationSucceeded(Guid memberId);
}
