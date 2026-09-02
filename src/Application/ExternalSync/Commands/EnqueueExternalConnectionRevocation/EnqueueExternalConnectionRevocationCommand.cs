using Application.Shared;

namespace Application.ExternalSync.Commands.EnqueueExternalConnectionRevocation;

/// <summary>Enqueues provider-originated connection revocation for background processing.</summary>
/// <param name="ProviderId">The external provider identifier.</param>
/// <param name="ExternalUserId">The provider user identifier.</param>
/// <param name="IdempotencyKey">The stable webhook event key.</param>
public sealed record EnqueueExternalConnectionRevocationCommand(
    string ProviderId,
    string ExternalUserId,
    string IdempotencyKey) : ICommand;
