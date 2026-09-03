using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared;

/// <summary>
/// Abstraction for durable background work without coupling Application to a scheduler.
/// </summary>
public interface IBackgroundJobClient
{
    /// <summary>Schedules a member avatar download.</summary>
    /// <param name="memberId">The unique identifier of the member.</param>
    /// <param name="avatarUrl">The remote URL of the avatar image to download.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task EnqueueAvatarDownloadAsync(Guid memberId, Uri avatarUrl, CancellationToken cancellationToken);

    /// <summary>Schedules synchronization of a member's federated identities.</summary>
    /// <param name="memberId">The unique identifier of the member.</param>
    /// <param name="identifyName">The identifying name of the member.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task EnqueueIdentitySyncAsync(Guid memberId, string identifyName, CancellationToken cancellationToken);

    /// <summary>Enqueues or coalesces activity synchronization for one provider athlete.</summary>
    /// <param name="request">The job request payload containing provider, athlete, and trigger details.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task EnqueueExternalActivitySyncAsync(
        ExternalActivitySyncJobRequest request,
        CancellationToken cancellationToken);

    /// <summary>Enqueues domain revocation initiated by an external provider webhook.</summary>
    /// <param name="providerId">The unique identifier of the external provider.</param>
    /// <param name="externalUserId">The user identifier assigned by the external provider.</param>
    /// <param name="idempotencyKey">A stable key used to coalesce duplicate webhook triggers.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task EnqueueExternalConnectionRevocationAsync(
        string providerId,
        string externalUserId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>Cancels queued activity work for one provider athlete.</summary>
    /// <param name="providerId">The unique identifier of the external provider.</param>
    /// <param name="externalUserId">The user identifier assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task CancelExternalActivitySyncAsync(
        string providerId,
        string externalUserId,
        CancellationToken cancellationToken);
}
