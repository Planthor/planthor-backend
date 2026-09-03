using System.Globalization;
using Adapters.Strava.Client;
using Adapters.Strava.Mapping;
using Adapters.Strava.Persistence;
using Application.Dtos;
using Application.Interfaces;
using Application.Shared;
using NodaTime;

namespace Adapters.Strava;

/// <summary>
/// Translates Strava activity and operational persistence into the provider-neutral Application boundary.
/// </summary>
/// <param name="client">The API client used to communicate with Strava.</param>
/// <param name="tokenDb">The repository for reading and mutating Strava tokens and sync states.</param>
/// <param name="clock">The system clock used for timestamps and sync tracking.</param>
public sealed class StravaActivitySyncAdapter(
    IStravaApiClient client,
    StravaAdapterDatabase tokenDb,
    IClock clock) : IActivitySyncAdapter
{
    private readonly IStravaApiClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly StravaAdapterDatabase _tokenDb = tokenDb ?? throw new ArgumentNullException(nameof(tokenDb));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private const int PageSize = 200;

    /// <inheritdoc />
    public string ProviderId => "STRAVA";

    /// <inheritdoc />
    public async Task<ActivitySyncFetchResultDto> FetchActivitiesAsync(
        string externalUserId,
        Instant rangeStart,
        Instant rangeEnd,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);

        var token = await GetTokenAsync(externalUserId, cancellationToken);
        if (token is null)
        {
            return AuthorizationRequired();
        }

        var afterEpoch = Math.Max(
            token.LastSyncEpoch ?? rangeStart.ToUnixTimeSeconds() - 1,
            rangeStart.ToUnixTimeSeconds() - 1);
        var beforeEpoch = rangeEnd.ToUnixTimeSeconds() + 1;
        if (afterEpoch >= beforeEpoch)
        {
            return new ActivitySyncFetchResultDto(
                ActivitySyncOutcome.Success,
                [],
                WatermarkCandidate: rangeEnd);
        }

        var activities = new List<AdapterActivityDto>();
        var hasMorePages = true;
        var page = 1;
        
        while (hasMorePages)
        {
            var result = await _client.GetAthleteActivitiesPageAsync(
                token.Id,
                afterEpoch,
                beforeEpoch,
                page,
                PageSize,
                cancellationToken);
            if (result.Outcome != StravaApiOutcome.Success)
            {
                return MapFailure(result);
            }

            var pageActivities = result.Value ?? [];
            foreach (var activity in pageActivities)
            {
                if (MapActivity(activity) is { } mapped)
                {
                    activities.Add(mapped);
                }
            }

            if (pageActivities.Count < PageSize)
            {
                hasMorePages = false;
            }

            page++;
        }

        return new ActivitySyncFetchResultDto(
            ActivitySyncOutcome.Success,
            activities,
            WatermarkCandidate: rangeEnd);
    }

    /// <inheritdoc />
    public async Task<ActivitySyncFetchResultDto> FetchActivityAsync(
        string externalUserId,
        string externalActivityId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);
        ArgumentException.ThrowIfNullOrEmpty(externalActivityId);

        var token = await GetTokenAsync(externalUserId, cancellationToken);
        if (token is null)
        {
            return AuthorizationRequired();
        }

        var result = await _client.GetActivityAsync(token.Id, externalActivityId, cancellationToken);
        if (result.Outcome != StravaApiOutcome.Success)
        {
            return MapFailure(result);
        }

        var mapped = result.Value is null ? null : MapActivity(result.Value);
        return new ActivitySyncFetchResultDto(
            ActivitySyncOutcome.Success,
            mapped is null ? [] : [mapped]);
    }

    /// <inheritdoc />
    public Task MarkQueuedAsync(string externalUserId, string trigger, CancellationToken cancellationToken) =>
        MutateStatusAsync(externalUserId, token =>
        {
            token.SyncState = "queued";
            token.LastSyncTrigger = trigger;
            token.NextSyncAttemptAtUtc = null;
            token.SyncErrorCode = null;
            if (trigger == ExternalActivitySyncTrigger.Initial)
            {
                token.InitialSyncState = "queued";
            }
        }, cancellationToken);

    /// <inheritdoc />
    public Task MarkRunningAsync(string externalUserId, string trigger, CancellationToken cancellationToken) =>
        MutateStatusAsync(externalUserId, token =>
        {
            token.SyncState = "running";
            token.LastSyncTrigger = trigger;
            token.LastSyncStartedAtUtc = _clock.GetCurrentInstant().ToDateTimeOffset();
            token.NextSyncAttemptAtUtc = null;
            token.SyncErrorCode = null;
            if (trigger == ExternalActivitySyncTrigger.Initial)
            {
                token.InitialSyncState = "running";
            }
        }, cancellationToken);

    /// <inheritdoc />
    public Task MarkSucceededAsync(
        string externalUserId,
        string trigger,
        Instant? historicalWatermark,
        int logsCreated,
        CancellationToken cancellationToken) =>
        MutateStatusAsync(externalUserId, token =>
        {
            token.SyncState = "succeeded";
            token.LastSyncTrigger = trigger;
            token.LastSuccessfulSyncAtUtc = _clock.GetCurrentInstant().ToDateTimeOffset();
            token.NextSyncAttemptAtUtc = null;
            token.SyncErrorCode = null;
            token.ActivityLogsCreated += logsCreated;
            if (historicalWatermark is not null)
            {
                token.LastSyncEpoch = Math.Max(
                    token.LastSyncEpoch ?? long.MinValue,
                    historicalWatermark.Value.ToUnixTimeSeconds());
            }

            if (trigger == ExternalActivitySyncTrigger.Initial)
            {
                token.InitialSyncState = "succeeded";
            }
        }, cancellationToken);

    /// <inheritdoc />
    public Task MarkDeferredAsync(
        string externalUserId,
        string trigger,
        Instant retryAt,
        string errorCode,
        CancellationToken cancellationToken) =>
        MutateStatusAsync(externalUserId, token =>
        {
            token.SyncState = "deferred";
            token.LastSyncTrigger = trigger;
            token.NextSyncAttemptAtUtc = retryAt.ToDateTimeOffset();
            token.SyncErrorCode = errorCode;
            if (trigger == ExternalActivitySyncTrigger.Initial)
            {
                token.InitialSyncState = "deferred";
            }
        }, cancellationToken);

    /// <inheritdoc />
    public Task MarkFailedAsync(
        string externalUserId,
        string trigger,
        string errorCode,
        CancellationToken cancellationToken) =>
        MutateStatusAsync(externalUserId, token =>
        {
            token.SyncState = "failed";
            token.LastSyncTrigger = trigger;
            token.NextSyncAttemptAtUtc = null;
            token.SyncErrorCode = errorCode;
            if (trigger == ExternalActivitySyncTrigger.Initial)
            {
                token.InitialSyncState = "failed";
            }
        }, cancellationToken);

    /// <inheritdoc />
    public async Task<ExternalActivitySyncStatusDto?> GetSyncStatusAsync(
        string externalUserId,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(externalUserId, cancellationToken);
        return token is null
            ? null
            : new ExternalActivitySyncStatusDto(
                ProviderId,
                token.InitialSyncState,
                token.SyncState,
                token.LastSyncTrigger,
                token.LastSyncStartedAtUtc,
                token.LastSuccessfulSyncAtUtc,
                token.NextSyncAttemptAtUtc,
                token.SyncErrorCode);
    }

    /// <inheritdoc />
    public async Task DeleteOperationalDataAsync(
        string externalUserId,
        CancellationToken cancellationToken)
    {
        if (long.TryParse(externalUserId, NumberStyles.None, CultureInfo.InvariantCulture, out var athleteId))
        {
            await _tokenDb.DeleteByAthleteIdAsync(athleteId, cancellationToken);
        }
    }

    /// <summary>
    /// Helper method to mutate and persist the sync status of an athlete's token document.
    /// </summary>
    /// <param name="externalUserId">The external user ID (athlete ID) in Strava.</param>
    /// <param name="mutation">An action that updates the token document.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    private async Task MutateStatusAsync(
        string externalUserId,
        Action<StravaTokenDocument> mutation,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(externalUserId, cancellationToken);
        if (token is null)
        {
            return;
        }

        mutation(token);
        await _tokenDb.UpsertAsync(token, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user's Strava token document using their external user ID.
    /// </summary>
    /// <param name="externalUserId">The external user ID (athlete ID) in Strava.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The token document if found; otherwise, <c>null</c>.</returns>
    private async Task<StravaTokenDocument?> GetTokenAsync(
        string externalUserId,
        CancellationToken cancellationToken)
    {
        return long.TryParse(externalUserId, NumberStyles.None, CultureInfo.InvariantCulture, out var athleteId)
            ? await _tokenDb.GetByAthleteIdAsync(athleteId, cancellationToken)
            : null;
    }

    /// <summary>
    /// Maps a raw <see cref="StravaActivityResponse"/> into an <see cref="AdapterActivityDto"/>.
    /// </summary>
    /// <param name="activity">The raw activity from the Strava API.</param>
    /// <returns>The mapped activity, or <c>null</c> if the sport type is unsupported.</returns>
    private AdapterActivityDto? MapActivity(StravaActivityResponse activity)
    {
        var rawSportType = string.IsNullOrWhiteSpace(activity.SportType)
            ? activity.Type
            : activity.SportType;
        var canonicalSportTypeId = StravaSportTypeMapper.MapToCanonicalId(rawSportType);
        if (canonicalSportTypeId is null)
        {
            return null;
        }

        return new AdapterActivityDto(
            activity.Id.ToString(CultureInfo.InvariantCulture),
            ProviderId,
            canonicalSportTypeId,
            Instant.FromDateTimeOffset(activity.StartDate),
            activity.Distance);
    }

    /// <summary>
    /// Generates an <see cref="ActivitySyncFetchResultDto"/> indicating that authorization is required.
    /// </summary>
    /// <returns>A fetch result with the outcome <see cref="ActivitySyncOutcome.AuthorizationRequired"/>.</returns>
    private static ActivitySyncFetchResultDto AuthorizationRequired() => new(
        ActivitySyncOutcome.AuthorizationRequired,
        [],
        ErrorCode: "strava_token_missing");

    /// <summary>
    /// Maps a <see cref="StravaApiResult{T}"/> failure to a provider-neutral <see cref="ActivitySyncFetchResultDto"/>.
    /// </summary>
    /// <typeparam name="T">The type of the underlying API result value.</typeparam>
    /// <param name="result">The failed Strava API result.</param>
    /// <returns>A fetch result containing the mapped error code and retry information.</returns>
    private static ActivitySyncFetchResultDto MapFailure<T>(StravaApiResult<T> result) => new(
        result.Outcome.Id switch
        {
            "NOT_FOUND" => ActivitySyncOutcome.NotFound,
            "AUTHORIZATION_REQUIRED" => ActivitySyncOutcome.AuthorizationRequired,
            "RATE_LIMITED" => ActivitySyncOutcome.RateLimited,
            _ => ActivitySyncOutcome.TransientFailure
        },
        [],
        RetryAt: result.RetryAt,
        ErrorCode: result.ErrorCode);
}
