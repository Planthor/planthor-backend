using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;

namespace Application.ExternalSync.Commands.EnqueueExternalConnectionRevocation;

/// <summary>
/// Schedules provider-originated revocation without doing domain work on the webhook path.
/// </summary>
/// <param name="backgroundJobClient">The client used to enqueue the background job for connection revocation.</param>
public sealed class EnqueueExternalConnectionRevocationCommandHandler(IBackgroundJobClient backgroundJobClient)
    : ICommandHandler<EnqueueExternalConnectionRevocationCommand>
{
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient 
        ?? throw new ArgumentNullException(nameof(backgroundJobClient));

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public Task Handle(EnqueueExternalConnectionRevocationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _backgroundJobClient.EnqueueExternalConnectionRevocationAsync(
            request.ProviderId,
            request.ExternalUserId,
            request.IdempotencyKey,
            cancellationToken);
    }
}
