using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;

namespace Application.ExternalSync.Commands.EnqueueExternalConnectionRevocation;

/// <summary>Schedules provider-originated revocation without doing domain work on the webhook path.</summary>
public sealed class EnqueueExternalConnectionRevocationCommandHandler(IBackgroundJobClient backgroundJobClient)
    : ICommandHandler<EnqueueExternalConnectionRevocationCommand>
{
    /// <inheritdoc />
    public Task Handle(EnqueueExternalConnectionRevocationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return backgroundJobClient.EnqueueExternalConnectionRevocationAsync(
            request.ProviderId,
            request.ExternalUserId,
            request.IdempotencyKey,
            cancellationToken);
    }
}
