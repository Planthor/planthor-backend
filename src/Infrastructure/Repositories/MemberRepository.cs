using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Members;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class MemberRepository(PlanthorDbContext context) : BaseRepository<Member>(context), IMemberRepository
{
    public async Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Members.FindAsync([id], cancellationToken);
    }
}
