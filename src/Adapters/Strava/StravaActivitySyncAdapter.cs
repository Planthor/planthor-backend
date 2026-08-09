using Application.Dtos;
using Application.Interfaces;
using Adapters.Strava.Client;
using NodaTime;

using Adapters.Strava.Persistence;

namespace Adapters.Strava;

/// <summary>
/// Implements <see cref="IActivitySyncAdapter"/> for the Strava fitness platform.
/// Fetches activities via the Strava API and maps them to the provider-agnostic
/// <see cref="AdapterActivityDto"/> shape.
/// </summary>
public sealed class StravaActivitySyncAdapter(IStravaApiClient client, StravaAdapterDatabase tokenDb) : IActivitySyncAdapter
{
    /// <summary>
    /// Gets the provider ID for Strava.
    /// </summary>
    public string ProviderId => "STRAVA";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesAsync(
        Guid memberId,
        string identifyName,
        Instant? since = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        var tokenDoc = await tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
        if (tokenDoc == null)
        {
            return [];
        }

        long sinceEpoch = tokenDoc.LastSyncEpoch ?? Instant.FromUtc(2026, 6, 1, 0, 0).ToUnixTimeSeconds();

        // If the application passed a specific `since` that is strictly newer than our watermark, we could optionally use it,
        // but typically we rely entirely on the tokenDoc's watermark as the single source of truth for incremental sync.
        if (since.HasValue && since.Value.ToUnixTimeSeconds() > sinceEpoch)
        {
            sinceEpoch = since.Value.ToUnixTimeSeconds();
        }

        var activities = new List<AdapterActivityDto>();
        long maxEpoch = sinceEpoch;

        int page = 1;
        while (true)
        {
            var stravaActivities = await client.GetAthleteActivitiesAsync(identifyName, sinceEpoch, page, 100, cancellationToken);
            if (stravaActivities.Count == 0)
                break;

            foreach (var sa in stravaActivities)
            {
                var dto = new AdapterActivityDto(
                    ExternalActivityId: sa.Id.ToString(),
                    ProviderId: ProviderId,
                    Name: string.IsNullOrEmpty(sa.Name) ? "Strava Activity" : sa.Name,
                    OccurredAt: Instant.FromDateTimeUtc(sa.StartDate.ToUniversalTime()),
                    ActivityType: string.IsNullOrEmpty(sa.SportType) ? sa.Type : sa.SportType,
                    DistanceMeters: sa.Distance,
                    MovingTime: null
                );

                activities.Add(dto);

                long actEpoch = dto.OccurredAt.ToUnixTimeSeconds();
                if (actEpoch > maxEpoch)
                {
                    maxEpoch = actEpoch;
                }
            }

            if (stravaActivities.Count < 100)
                break; // Last page

            page++;
        }

        if (maxEpoch > sinceEpoch)
        {
            tokenDoc.LastSyncEpoch = maxEpoch;
            await tokenDb.UpsertAsync(tokenDoc, cancellationToken);
        }

        return activities;
    }
}
