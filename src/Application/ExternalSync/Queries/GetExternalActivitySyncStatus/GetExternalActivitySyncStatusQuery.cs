using Application.Dtos;
using Application.Shared;

namespace Application.ExternalSync.Queries.GetExternalActivitySyncStatus;

/// <summary>Gets activity synchronization status for an authenticated connection owner.</summary>
/// <param name="Identifier">The route member identifier, normally <c>me</c>.</param>
/// <param name="CurrentIdentifyName">The authenticated identity name.</param>
/// <param name="ProviderId">The external provider identifier.</param>
public sealed record GetExternalActivitySyncStatusQuery(
    string Identifier,
    string CurrentIdentifyName,
    string ProviderId) : IQuery<ExternalActivitySyncStatusDto>;
