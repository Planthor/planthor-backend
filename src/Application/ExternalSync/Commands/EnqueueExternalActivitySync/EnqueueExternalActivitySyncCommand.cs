using Application.Shared;

namespace Application.ExternalSync.Commands.EnqueueExternalActivitySync;

/// <summary>
/// Enqueues provider-neutral activity work when the provider athlete is already known.
/// </summary>
/// <param name="ProviderId">The external provider identifier.</param>
/// <param name="ExternalUserId">The athlete identifier assigned by the provider.</param>
/// <param name="Trigger">The synchronization trigger kind.</param>
/// <param name="IdempotencyKey">The stable event or request key.</param>
/// <param name="ExternalActivityId">An optional single activity identifier.</param>
public sealed record EnqueueExternalActivitySyncCommand(
    string ProviderId,
    string ExternalUserId,
    string Trigger,
    string IdempotencyKey,
    string? ExternalActivityId = null) : ICommand;
