using Adapters.Strava.Configuration;
using Application.Shared;
using Domain.Members;
using Domain.Members.Events;
using Microsoft.Extensions.Options;

namespace Adapters.Strava.EventHandlers;

/// <summary>
/// Schedules the initial historical activity import after a Strava connection is successfully established.
/// </summary>
/// <param name="activitySyncAdapter">The adapter used to mark the activity synchronization process as queued.</param>
/// <param name="backgroundJobClient">The client used to enqueue the background job for the activity synchronization.</param>
/// <param name="options">The Strava configuration options, used to check if automatic synchronization is enabled.</param>
public sealed class StravaConnectionEstablishedEventHandler(
    StravaActivitySyncAdapter activitySyncAdapter,
    IBackgroundJobClient backgroundJobClient,
    IOptions<StravaOptions> options)
    : IDomainEventHandler<ExternalConnectionEstablishedEvent>
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public async Task HandleAsync(
        ExternalConnectionEstablishedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (!options.Value.AutomaticSyncEnabled ||
            domainEvent.Provider != ExternalProvider.Strava ||
            domainEvent.Type != ExternalConnectionType.ActivitiesSync)
        {
            return;
        }

        await activitySyncAdapter.MarkQueuedAsync(
            domainEvent.ExternalUserId,
            ExternalActivitySyncTrigger.Initial,
            cancellationToken);
        await backgroundJobClient.EnqueueExternalActivitySyncAsync(
            new ExternalActivitySyncJobRequest(
                ExternalProvider.Strava.Id,
                domainEvent.ExternalUserId,
                ExternalActivitySyncTrigger.Initial,
                $"initial:{domainEvent.ExternalConnectionId}"),
            cancellationToken);
    }
}
