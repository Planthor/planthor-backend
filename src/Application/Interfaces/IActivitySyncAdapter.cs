using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using NodaTime;

namespace Application.Interfaces;

/// <summary>
/// Provider-neutral boundary for external activity synchronization.
/// Implementations translate provider contracts before data crosses into Application.
/// </summary>
public interface IActivitySyncAdapter
{
    /// <summary>Gets the external provider identifier served by this adapter.</summary>
    string ProviderId { get; }

    /// <summary>Fetches provider activities in a frozen, bounded historical range.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="rangeStart">The start of the historical range (inclusive).</param>
    /// <param name="rangeEnd">The end of the historical range (inclusive).</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A fetch result containing the mapped activities or error details.</returns>
    Task<ActivitySyncFetchResultDto> FetchActivitiesAsync(
        string externalUserId,
        Instant rangeStart,
        Instant rangeEnd,
        CancellationToken cancellationToken);

    /// <summary>Fetches one external activity referenced by a real-time webhook.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="externalActivityId">The identifier of the specific activity in the external provider's system.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A fetch result containing the single mapped activity or error details.</returns>
    Task<ActivitySyncFetchResultDto> FetchActivityAsync(
        string externalUserId,
        string externalActivityId,
        CancellationToken cancellationToken);

    /// <summary>Marks synchronization as queued without fetching provider data.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="trigger">The trigger that initiated the sync (e.g., manual, webhook, initial).</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkQueuedAsync(string externalUserId, string trigger, CancellationToken cancellationToken);

    /// <summary>Marks synchronization as actively processing.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="trigger">The trigger that initiated the sync (e.g., manual, webhook, initial).</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkRunningAsync(string externalUserId, string trigger, CancellationToken cancellationToken);

    /// <summary>
    /// Commits operational success and advances a historical watermark only after Plan writes commit.
    /// </summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="trigger">The trigger that initiated the sync.</param>
    /// <param name="historicalWatermark">An optional timestamp representing the new successful historical watermark to advance to.</param>
    /// <param name="logsCreated">The number of activity logs successfully created during this run.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkSucceededAsync(
        string externalUserId,
        string trigger,
        Instant? historicalWatermark,
        int logsCreated,
        CancellationToken cancellationToken);

    /// <summary>Marks synchronization as deferred until a later instant.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="trigger">The trigger that initiated the sync.</param>
    /// <param name="retryAt">The exact instant when the synchronization should be retried.</param>
    /// <param name="errorCode">A stable machine-readable code explaining why the sync was deferred.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkDeferredAsync(
        string externalUserId,
        string trigger,
        Instant retryAt,
        string errorCode,
        CancellationToken cancellationToken);

    /// <summary>Marks synchronization as terminally failed with a stable error code.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="trigger">The trigger that initiated the sync.</param>
    /// <param name="errorCode">A stable machine-readable code explaining the failure.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkFailedAsync(
        string externalUserId,
        string trigger,
        string errorCode,
        CancellationToken cancellationToken);

    /// <summary>Gets the adapter-owned operational sync status for an athlete.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The synchronization status DTO, or <c>null</c> if no state exists.</returns>
    Task<ExternalActivitySyncStatusDto?> GetSyncStatusAsync(
        string externalUserId,
        CancellationToken cancellationToken);

    /// <summary>Deletes tokens and all adapter-owned operational state for an athlete.</summary>
    /// <param name="externalUserId">The identifier of the user in the external provider's system.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task DeleteOperationalDataAsync(string externalUserId, CancellationToken cancellationToken);
}
