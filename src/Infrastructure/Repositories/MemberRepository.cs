using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Members;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MemberRepository(PlanthorDbContext context) : BaseRepository<Member>(context), IMemberRepository
{
    public async Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Members.FindAsync([id], cancellationToken);
    }

    public async Task<Member?> GetByIdentifyNameAsync(string identifyName, CancellationToken cancellationToken)
    {
        return await Context.Members.FirstOrDefaultAsync(m => m.IdentifyName == identifyName, cancellationToken);
    }
}
