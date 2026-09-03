using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Adapters.Strava.Coordinator;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Adapters.Strava.Client;

/// <summary>
/// Encapsulates the execution pipeline for Strava API requests.
/// </summary>
/// <typeparam name="T">The expected response payload type.</typeparam>
/// <remarks>
/// In the context of Clean Architecture and DDD, this class acts as an infrastructural resilient wrapper 
/// (similar to a specialized <see cref="DelegatingHandler"/> or API Gateway policy) specifically designed 
/// for the Strava integration. 
/// 
/// It orchestrates cross-cutting integration concerns such as:
/// - Transparent OAuth token injection and proactive/reactive refresh flows.
/// - Upstream rate limit observation and propagation to the <see cref="StravaRateLimitCoordinator"/>.
/// - Fault tolerance and translation of HTTP anomalies (network errors, JSON deserialization failures) 
///   into deterministic, typed <see cref="StravaApiResult{T}"/> outcomes.
/// 
/// By isolating this orchestration, <see cref="StravaApiClient"/> is decoupled from retry/token logic 
/// and acts purely as an API façade and route definition layer.
/// </remarks>
/// <param name="httpClient">The HTTP client used to communicate with the Strava API.</param>
/// <param name="tokenClient">The token client responsible for fetching and refreshing OAuth tokens.</param>
/// <param name="clock">The system clock used to calculate dynamic retry delay intervals.</param>
/// <param name="rateLimitCoordinator">The coordinator that tracks and governs global rate limit thresholds.</param>
/// <param name="logger">The logger instance for tracking request failures.</param>
internal sealed partial class RequestRunner<T>(
    HttpClient httpClient,
    StravaTokenClient tokenClient,
    IClock clock,
    StravaRateLimitCoordinator rateLimitCoordinator,
    ILogger logger)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly StravaTokenClient _tokenClient = tokenClient ?? throw new ArgumentNullException(nameof(tokenClient));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly StravaRateLimitCoordinator _rateLimitCoordinator = rateLimitCoordinator ?? throw new ArgumentNullException(nameof(rateLimitCoordinator));

    public async Task<StravaApiResult<T>> SendAuthorizedAsync(
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
            _rateLimitCoordinator.Observe(firstResponse);
            if (firstResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                return await ReadActivityResponseAsync(firstResponse, cancellationToken);
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
            _rateLimitCoordinator.Observe(retryResponse);
            return await ReadActivityResponseAsync(retryResponse, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogActivityRequestFailed(exception, identifyName);
            return TransientFailure();
        }
        catch (JsonException exception)
        {
            LogActivityRequestFailed(exception, identifyName);
            return TransientFailure();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogActivityRequestFailed(exception, identifyName);
            return TransientFailure();
        }
    }

    private async Task<StravaApiResult<T>> ReadActivityResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null
                ? TransientFailure()
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
                RetryAt: _rateLimitCoordinator.GetRetryAt(),
                ErrorCode: "strava_rate_limited"),
            _ => TransientFailure()
        };
    }

    public StravaApiResult<T> TransientFailure() => new(
        StravaApiOutcome.TransientFailure,
        RetryAt: _clock.GetCurrentInstant().Plus(Duration.FromMinutes(1)),
        ErrorCode: "strava_temporarily_unavailable");

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava activity request failed for member {IdentifyName}")]
    private partial void LogActivityRequestFailed(Exception exception, string identifyName);
}
