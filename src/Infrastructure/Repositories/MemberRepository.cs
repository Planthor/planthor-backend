using System.Threading;
using System.Threading.Tasks;
using Domain.Members;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MemberRepository(PlanthorDbContext context) : BaseRepository<Member>(context), IMemberRepository
{
    public async Task<bool> AnyAsync(string identifyName, CancellationToken cancellationToken)
    {
        return await Context.Members.AnyAsync(m => m.IdentifyName == identifyName, cancellationToken);
    }
}
