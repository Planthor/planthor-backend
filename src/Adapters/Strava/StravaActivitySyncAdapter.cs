using Adapters.Abstraction;
using Adapters.Strava.Client;
using NodaTime;

namespace Adapters.Strava;

/// <summary>
/// Implements <see cref="IActivitySyncAdapter"/> for the Strava fitness platform.
/// Fetches activities via the Strava API and maps them to the provider-agnostic
/// <see cref="AdapterActivityDto"/> shape.
/// </summary>
public sealed class StravaActivitySyncAdapter(StravaApiClient client) : IActivitySyncAdapter
{
    public string ProviderId => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesAsync(
        Guid memberId,
        Instant since,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
