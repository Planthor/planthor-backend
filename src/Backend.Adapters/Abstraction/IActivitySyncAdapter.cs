using NodaTime;

namespace Backend.Adapters.Abstraction;

/// <summary>
/// Provider-agnostic contract for fetching external activity data on behalf of a member.
/// Implementations are registered with keyed DI using <see cref="ProviderId"/> as the key.
/// </summary>
public interface IActivitySyncAdapter
{
    /// <summary>
    /// The external provider this adapter serves (matches <c>ExternalProvider.Id</c>).
    /// </summary>
    /// <example>"STRAVA" | "GITHUB"</example>
    string ProviderId { get; }

    /// <summary>
    /// Fetches activities for a member that occurred after <paramref name="since"/>.
    /// Returns an empty list if the member has no active connection for this provider.
    /// </summary>
    Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesAsync(
        Guid memberId,
        Instant since,
        CancellationToken cancellationToken);
}
