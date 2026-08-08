using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.List;

/// <summary>
/// Handler for retrieving all members.
/// </summary>
public class ListMembersQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<ListMembersQuery, IEnumerable<MemberDto>>
{
    /// <inheritdoc />
    public Task<IEnumerable<MemberDto>> Handle(ListMembersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<IEnumerable<MemberDto>> Core()
        {
            return await readOnlyContext.QueryAsync<Member, MemberDto>(
                q => q.Select(m => new MemberDto(
                    m.Id,
                    m.FirstName,
                    m.MiddleName,
                    m.LastName,
                    m.Description,
                    m.PathAvatar ?? string.Empty
                )),
                cancellationToken);
        }
    }
}
