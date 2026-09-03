using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Interfaces;
using Application.Shared;
using Domain.Members;

namespace Application.ExternalSync.Queries.GetExternalActivitySyncStatus;

/// <summary>
/// Enforces connection ownership before returning adapter operational state.
/// </summary>
/// <param name="memberRepository">The repository for accessing member data.</param>
/// <param name="activitySyncAdapters">The collection of available activity sync adapters.</param>
public sealed class GetExternalActivitySyncStatusQueryHandler(
    IMemberRepository memberRepository,
    IEnumerable<IActivitySyncAdapter> activitySyncAdapters)
    : IQueryHandler<GetExternalActivitySyncStatusQuery, ExternalActivitySyncStatusDto>
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the specified member, connection, or adapter is not found.</exception>
    public async Task<ExternalActivitySyncStatusDto> Handle(
        GetExternalActivitySyncStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await memberRepository.GetByIdentifyNameAsync(
            request.CurrentIdentifyName,
            cancellationToken)
            ?? throw new KeyNotFoundException($"Member '{request.CurrentIdentifyName}' was not found.");

        if (!request.Identifier.Equals("me", StringComparison.OrdinalIgnoreCase) &&
            (!Guid.TryParse(request.Identifier, out var requestedMemberId) || requestedMemberId != member.Id))
        {
            throw new KeyNotFoundException("External activity sync status was not found.");
        }

        var connection = member.ExternalConnections.FirstOrDefault(candidate =>
            candidate.Provider.Id.Equals(request.ProviderId, StringComparison.OrdinalIgnoreCase) &&
            candidate.Type == ExternalConnectionType.ActivitiesSync &&
            candidate.Status == ConnectionStatus.Active)
            ?? throw new KeyNotFoundException("External activity connection was not found.");

        var adapter = activitySyncAdapters.FirstOrDefault(candidate =>
            candidate.ProviderId.Equals(request.ProviderId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException("External activity adapter was not found.");

        return await adapter.GetSyncStatusAsync(connection.ExternalUserId, cancellationToken)
            ?? new ExternalActivitySyncStatusDto(
                request.ProviderId,
                InitialSyncState: "not_started",
                State: "idle",
                LastTrigger: null,
                LastStartedAt: null,
                LastSuccessfulSyncAt: null,
                NextAttemptAt: null,
                ErrorCode: null);
    }
}
