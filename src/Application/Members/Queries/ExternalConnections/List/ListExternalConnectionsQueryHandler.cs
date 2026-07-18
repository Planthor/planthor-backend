using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.ExternalConnections.List;

/// <summary>
/// Handler for retrieving the list of external connections for a member.
/// </summary>
/// <param name="readOnlyContext">The read-only context.</param>
public class ListExternalConnectionsQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<ListExternalConnectionsQuery, IEnumerable<ExternalConnectionDto>>
{
    /// <inheritdoc />
    public async Task<IEnumerable<ExternalConnectionDto>> Handle(ListExternalConnectionsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dtos = await readOnlyContext.QueryAsync<Member, ExternalConnectionDto>(
            q => {
                var memberQuery = q;
                if (request.Identifier.Equals("@me", StringComparison.OrdinalIgnoreCase))
                {
                    memberQuery = memberQuery.Where(m => m.IdentifyName == request.CurrentIdentifyName);
                }
                else if (Guid.TryParse(request.Identifier, out var memberId))
                {
                    memberQuery = memberQuery.Where(m => m.Id == memberId);
                }
                else
                {
                    // Invalid identifier format
                    memberQuery = memberQuery.Where(m => false);
                }

                return memberQuery
                    .SelectMany(m => m.ExternalConnections)
                    .Select(c => new ExternalConnectionDto(
                        c.Id,
                        c.MemberId,
                        c.Provider.Id,
                        c.Type.Id,
                        c.ExternalUserId,
                        c.Status.Id,
                        c.ConnectedAt,
                        c.DisconnectedAt));
            },
            cancellationToken);

        return dtos;
    }
}
