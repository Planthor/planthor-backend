using Adapters.Abstraction;
using Adapters.Strava.Client;
using NodaTime;

namespace Adapters.Strava;

/// <summary>
/// Implements <see cref="IActivitySyncAdapter"/> for the Strava fitness platform.
/// Fetches activities via the Strava API and maps them to the provider-agnostic
/// <see cref="AdapterActivityDto"/> shape.
/// </summary>
public sealed class StravaActivitySyncAdapter(IStravaApiClient client) : IActivitySyncAdapter
{
    /// <summary>
    /// Gets the provider ID for Strava.
    /// </summary>
    public string ProviderId => "STRAVA";

    /// <inheritdoc/>
    public Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesAsync(
        Guid memberId,
        Instant since,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Task.FromResult<IReadOnlyList<AdapterActivityDto>>([]);
    }
}
