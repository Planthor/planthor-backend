using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.Details;

/// <summary>
/// Handler for retrieving the details of a member.
/// </summary>
public class MemberDetailsQueryHandler : IQueryHandler<MemberDetailsQuery, MemberDto>
{
    private readonly IReadOnlyContext _readOnlyContext;

    public MemberDetailsQueryHandler(IReadOnlyContext readOnlyContext)
    {
        ArgumentNullException.ThrowIfNull(readOnlyContext);
        _readOnlyContext = readOnlyContext;
    }

    /// <inheritdoc />
    public Task<MemberDto> Handle(MemberDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<MemberDto> Core()
        {
            var memberDto = await _readOnlyContext.FirstOrDefaultAsync<Member, MemberDto>(
                q => q.Where(m => m.Id == request.Id)
                    .Select(m => new MemberDto(
                        m.Id,
                        m.FirstName,
                        m.MiddleName,
                        m.LastName,
                        m.Description,
                        m.PathAvatar ?? string.Empty
                    )),
                cancellationToken);

            if (memberDto == null)
            {
                throw new KeyNotFoundException($"Member with ID '{request.Id}' was not found.");
            }

            return memberDto;
        }
    }
}
