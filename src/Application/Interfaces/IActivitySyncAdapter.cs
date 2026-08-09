using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;

using Application.Dtos;

namespace Application.Interfaces;

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
    /// <param name="identifyName">The member's identify name (e.g. from identity provider).</param>
    /// <param name="since">Optional start instant. If null, the adapter manages its own watermark.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, returning a read-only list of activity DTOs.</returns>
    Task<IReadOnlyList<AdapterActivityDto>> FetchActivitiesAsync(
        Guid memberId,
        string identifyName,
        Instant? since = null,
        CancellationToken cancellationToken = default);
}
