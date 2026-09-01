using System.Globalization;
using Adapters.Strava.Client;
using Adapters.Strava.Persistence;
using Application.Dtos;
using Application.Interfaces;
using NodaTime;

namespace Adapters.Strava;

/// <summary>
/// Implements <see cref="IActivitySyncAdapter"/> for the Strava fitness platform.
/// Fetches activities via the Strava API and maps them to the provider-agnostic
/// <see cref="AdapterActivityDto"/> shape.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StravaActivitySyncAdapter"/> class.
/// </remarks>
/// <param name="client">The Strava API client to use for fetching activities.</param>
/// <param name="tokenDb">The database used for managing Strava sync tokens.</param>
public sealed class StravaActivitySyncAdapter(IStravaApiClient client, StravaAdapterDatabase tokenDb) : IActivitySyncAdapter
{
    private const int PageSize = 100;
    private readonly IStravaApiClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly StravaAdapterDatabase _tokenDb = tokenDb ?? throw new ArgumentNullException(nameof(tokenDb));

    /// <summary>
    /// Gets the provider ID for Strava.
    /// </summary>
    public string ProviderId => "STRAVA";

    /// <inheritdoc/>
    public Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesAsync(
        Guid memberId,
        string identifyName,
        Instant? since,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifyName);

        return FetchActivitiesInternalAsync(identifyName, since, cancellationToken);
    }

    private async Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesInternalAsync(
        string identifyName,
        Instant? since,
        CancellationToken cancellationToken)
    {
        var tokenDoc = await _tokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken);
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
            var stravaActivities = await _client.GetAthleteActivitiesAsync(identifyName, sinceEpoch, page, PageSize, cancellationToken);
            if (stravaActivities.Count == 0)
            {
                break;
            }

            foreach (var sa in stravaActivities)
            {
                var dto = new AdapterActivityDto(
                    ExternalActivityId: sa.Id.ToString(CultureInfo.InvariantCulture),
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

            if (stravaActivities.Count < PageSize)
            {
                break; // Last page
            }

            page++;
        }

        if (maxEpoch > sinceEpoch)
        {
            tokenDoc.LastSyncEpoch = maxEpoch;
            await _tokenDb.UpsertAsync(tokenDoc, cancellationToken);
        }

        return activities;
    }
}
