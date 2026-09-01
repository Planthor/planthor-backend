using System.Net.Http.Json;
using Adapters.Strava.Configuration;
using Adapters.Strava.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

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
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="StravaApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="tokenDb">The Strava token database.</param>
    /// <param name="options">The Strava options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="clock">The clock instance.</param>
    public StravaApiClient(
        HttpClient httpClient,
        StravaAdapterDatabase tokenDb,
        IOptions<StravaOptions> options,
        ILogger<StravaApiClient> logger,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(tokenDb);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(clock);

        _httpClient = httpClient;
        _tokenDb = tokenDb;
        _options = options.Value;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// Gets the Strava OAuth token endpoint URI.
    /// </summary>
    private Uri TokenEndpoint => new(_options.BaseUrl, "oauth/token");

    /// <summary>
    /// Gets the Strava OAuth deauthorize endpoint URI.
    /// </summary>
    private Uri DeauthorizeEndpoint => new(_options.BaseUrl, "oauth/deauthorize");

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
    public Task<StravaTokenResponse?> ExchangeCodeAsync(
        string code,
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(identifyName);

        return Core();

        async Task<StravaTokenResponse?> Core()
        {
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
                LastRefreshedAtUtc = _clock.GetCurrentInstant().ToDateTimeUtc()
            };

            await _tokenDb.UpsertAsync(document, cancellationToken);

            LogTokenExchangeSucceeded(identifyName, tokenResponse.Athlete.Id);
            return tokenResponse;
        }
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
    public Task<StravaTokenDocument?> RefreshTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        return Core();

        async Task<StravaTokenDocument?> Core()
        {
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
            existing.LastRefreshedAtUtc = _clock.GetCurrentInstant().ToDateTimeUtc();

            await _tokenDb.UpsertAsync(existing, cancellationToken);

            LogTokenRefreshSucceeded(identifyName);
            return existing;
        }
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
    public Task<StravaTokenDocument?> GetValidTokenAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        return Core();

        async Task<StravaTokenDocument?> Core()
        {
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
    }

    /// <summary>
    /// Deauthorizes the application from the member's Strava account
    /// and removes the stored tokens.
    /// </summary>
    /// <param name="identifyName">The Planthor member identifier to deauthorize.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><c>true</c> if deauthorization succeeded or token was already removed; otherwise <c>false</c>.</returns>
    public Task<bool> DeauthorizeAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        return Core();

        async Task<bool> Core()
        {
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
    }

    // ────────────────────────────────────────────────────────────────
    // Athlete Activities
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves a paginated list of activities for the specified member.
    /// </summary>
    /// <param name="identifyName">The Planthor member identifier.</param>
    /// <param name="afterEpoch">An epoch timestamp to use for filtering activities that have taken place after a certain time.</param>
    /// <param name="page">Page number to fetch.</param>
    /// <param name="perPage">Number of items per page.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A list of <see cref="StravaActivityResponse"/>.</returns>
    public Task<IReadOnlyList<StravaActivityResponse>> GetAthleteActivitiesAsync(
        string identifyName,
        long? afterEpoch,
        int page,
        int perPage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);
        
        return Core();

        async Task<IReadOnlyList<StravaActivityResponse>> Core()
        {
            var token = await GetValidTokenAsync(identifyName, cancellationToken);
            if (token is null)
            {
                LogNoValidTokenForActivities(identifyName);
                return [];
            }

            var requestUri = $"athlete/activities?page={page}&per_page={perPage}";
            if (afterEpoch.HasValue)
            {
                requestUri += $"&after={afterEpoch.Value}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.BaseUrl, requestUri));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                LogFetchActivitiesFailed(identifyName, response.StatusCode);
                return [];
            }

            var activities = await response.Content.ReadFromJsonAsync<List<StravaActivityResponse>>(cancellationToken);
            return activities ?? (IReadOnlyList<StravaActivityResponse>)[];
        }
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot fetch activities: no valid token for member {IdentifyName}")]
    private partial void LogNoValidTokenForActivities(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch activities for {IdentifyName}. Status: {StatusCode}")]
    private partial void LogFetchActivitiesFailed(string identifyName, System.Net.HttpStatusCode statusCode);
}
