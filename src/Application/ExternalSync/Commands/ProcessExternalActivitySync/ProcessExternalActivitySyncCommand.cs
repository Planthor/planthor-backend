using Application.Shared;

namespace Application.ExternalSync.Commands.ProcessExternalActivitySync;

/// <summary>Processes a provider-neutral background activity synchronization run.</summary>
/// <param name="Request">The scheduler payload.</param>
public sealed record ProcessExternalActivitySyncCommand(ExternalActivitySyncJobRequest Request)
    : ICommand<ProcessExternalActivitySyncResult>;
