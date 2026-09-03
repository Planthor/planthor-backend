using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Interfaces;
using Application.Shared;
using Domain.Members;

namespace Application.ExternalSync.Commands.RequestExternalActivitySync;

/// <summary>
/// Resolves an owned active connection and coalesces a manual background sync.
/// </summary>
/// <param name="memberRepository">The repository for accessing member data.</param>
/// <param name="activitySyncAdapters">The collection of available activity sync adapters.</param>
/// <param name="backgroundJobClient">The background job client.</param>
public sealed class RequestExternalActivitySyncCommandHandler(
    IMemberRepository memberRepository,
    IEnumerable<IActivitySyncAdapter> activitySyncAdapters,
    IBackgroundJobClient backgroundJobClient)
    : ICommandHandler<RequestExternalActivitySyncCommand, ActivitySyncEnqueueResultDto>
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the specified member is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no active external activity connection exists, or no activity adapter is registered for the requested provider.</exception>
    public Task<ActivitySyncEnqueueResultDto> Handle(
        RequestExternalActivitySyncCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return HandleAsync(request, cancellationToken);
    }

    private async Task<ActivitySyncEnqueueResultDto> HandleAsync(
        RequestExternalActivitySyncCommand request,
        CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken)
            ?? throw new KeyNotFoundException($"Member '{request.IdentifyName}' was not found.");

        var connection = member.ExternalConnections.FirstOrDefault(candidate =>
            candidate.Provider.Id.Equals(request.ProviderId, StringComparison.OrdinalIgnoreCase) &&
            candidate.Type == ExternalConnectionType.ActivitiesSync &&
            candidate.Status == ConnectionStatus.Active)
            ?? throw new InvalidOperationException("No active external activity connection exists.");

        var adapter = activitySyncAdapters.FirstOrDefault(candidate =>
            candidate.ProviderId.Equals(request.ProviderId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No activity adapter is registered for '{request.ProviderId}'.");

        await adapter.MarkQueuedAsync(
            connection.ExternalUserId,
            ExternalActivitySyncTrigger.Manual,
            cancellationToken);

        await backgroundJobClient.EnqueueExternalActivitySyncAsync(
            new ExternalActivitySyncJobRequest(
                request.ProviderId,
                connection.ExternalUserId,
                ExternalActivitySyncTrigger.Manual,
                $"manual:{request.ProviderId}:{connection.ExternalUserId}"),
            cancellationToken);

        return new ActivitySyncEnqueueResultDto(request.ProviderId, "queued");
    }
}
