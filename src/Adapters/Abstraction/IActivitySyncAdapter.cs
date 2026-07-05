using NodaTime;

namespace Adapters.Abstraction;

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
    /// Fetches activities for a member that occurred after <paramref name="since"/>, with support for cancellation.
    /// Returns an empty list if the member has no active connection for this provider.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member.</param>
    /// <param name="since">The start instant from which to fetch activities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, returning a read-only list of activity DTOs.</returns>
    Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesAsync(
        Guid memberId,
        Instant since,
        CancellationToken cancellationToken);
}
