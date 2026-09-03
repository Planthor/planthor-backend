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
public sealed class ExternalConnectionDetailsQueryHandler : IQueryHandler<ExternalConnectionDetailsQuery, ExternalConnectionDto>
{
    private readonly IReadOnlyContext _readOnlyContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalConnectionDetailsQueryHandler"/> class.
    /// </summary>
    public ExternalConnectionDetailsQueryHandler(IReadOnlyContext readOnlyContext)
    {
        ArgumentNullException.ThrowIfNull(readOnlyContext);
        _readOnlyContext = readOnlyContext;
    }

    /// <inheritdoc />
    public Task<ExternalConnectionDto> Handle(ExternalConnectionDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<ExternalConnectionDto> Core()
        {
            var member = await _readOnlyContext.FirstOrDefaultAsync<Member, Member>(
                q => {
                    var memberQuery = q;
                    if (request.Identifier.Equals("me", StringComparison.OrdinalIgnoreCase))
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

                    return memberQuery;
                },
                cancellationToken);

            var connection = (member?.ExternalConnections.FirstOrDefault(c => c.Id == request.ConnectionId)) 
                ?? throw new KeyNotFoundException($"External connection '{request.ConnectionId}' for member '{request.Identifier}' was not found.");
            
            return new ExternalConnectionDto(
                connection.Id,
                connection.MemberId,
                connection.Provider.Id,
                connection.Type.Id,
                connection.ExternalUserId,
                connection.Status.Id,
                connection.ConnectedAt,
                connection.DisconnectedAt);
        }
    }
}
