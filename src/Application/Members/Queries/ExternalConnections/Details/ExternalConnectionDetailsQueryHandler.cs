using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.ExternalConnections.Details;

/// <summary>
/// Handler for retrieving the details of a specific external connection.
/// </summary>
/// <param name="readOnlyContext">The read-only context.</param>
public class ExternalConnectionDetailsQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<ExternalConnectionDetailsQuery, ExternalConnectionDto>
{
    /// <inheritdoc />
    public async Task<ExternalConnectionDto> Handle(ExternalConnectionDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dto = await readOnlyContext.FirstOrDefaultAsync<Member, ExternalConnectionDto>(
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
                    .Where(c => c.Id == request.ConnectionId)
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

        if (dto == null)
        {
            throw new KeyNotFoundException($"External connection '{request.ConnectionId}' for member '{request.Identifier}' was not found.");
        }

        return dto;
    }
}
