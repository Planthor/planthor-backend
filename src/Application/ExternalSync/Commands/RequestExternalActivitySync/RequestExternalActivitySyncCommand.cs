using Application.Dtos;
using Application.Shared;

namespace Application.ExternalSync.Commands.RequestExternalActivitySync;

/// <summary>
/// Requests a background activity sync for an authenticated Planthor member.
/// </summary>
/// <param name="IdentifyName">The authenticated member identity name.</param>
/// <param name="ProviderId">The external provider identifier.</param>
public sealed record RequestExternalActivitySyncCommand(string IdentifyName, string ProviderId)
    : ICommand<ActivitySyncEnqueueResultDto>;
