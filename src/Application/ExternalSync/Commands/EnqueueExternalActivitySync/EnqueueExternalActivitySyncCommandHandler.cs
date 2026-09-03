using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;

namespace Application.ExternalSync.Commands.EnqueueExternalActivitySync;

/// <summary>
/// Schedules provider-neutral activity work without doing domain work on the webhook path.
/// </summary>
/// <param name="backgroundJobClient">The background job client.</param>
public sealed class EnqueueExternalActivitySyncCommandHandler(IBackgroundJobClient backgroundJobClient)
    : ICommandHandler<EnqueueExternalActivitySyncCommand>
{
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
    /// <inheritdoc />
    public Task Handle(EnqueueExternalActivitySyncCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(_backgroundJobClient);

        return _backgroundJobClient.EnqueueExternalActivitySyncAsync(
            new ExternalActivitySyncJobRequest(
                request.ProviderId,
                request.ExternalUserId,
                request.Trigger,
                request.IdempotencyKey,
                request.ExternalActivityId),
            cancellationToken);
    }
}
