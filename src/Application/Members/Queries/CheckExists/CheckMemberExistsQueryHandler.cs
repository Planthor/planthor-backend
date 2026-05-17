using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.CheckExists;

public class CheckMemberExistsQueryHandler(IMemberRepository memberRepository)
    : IQueryHandler<CheckMemberExistsQuery, bool>
{
    public async Task<bool> Handle(CheckMemberExistsQuery request, CancellationToken cancellationToken)
    {
        return await memberRepository.AnyAsync(request.IdentifyName, cancellationToken);
    }
}
