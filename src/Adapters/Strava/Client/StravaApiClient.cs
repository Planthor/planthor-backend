using System.Globalization;
using System.Net.Http.Headers;
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
internal sealed class StravaApiClient(
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
    private readonly StravaRateLimitCoordinator _rateLimitCoordinator = rateLimitCoordinator 
        ?? throw new ArgumentNullException(nameof(rateLimitCoordinator));

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
        if (_rateLimitCoordinator.GetHistoricalDeferral() is { } deferredUntil)
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

        var requestRunner = new RequestRunner<IReadOnlyList<StravaActivityResponse>>(
            _httpClient, _tokenClient, _clock, _rateLimitCoordinator, logger);

        return requestRunner.SendAuthorizedAsync(
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

        var requestRunner = new RequestRunner<StravaActivityResponse>(
            _httpClient, _tokenClient, _clock, _rateLimitCoordinator, logger);

        var result = await requestRunner.SendAuthorizedAsync(
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

    private HttpRequestMessage CreateAuthorizedGet(Uri relativeUri, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.BaseUrl, relativeUri));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
