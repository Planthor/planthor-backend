using Domain.Plans;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class PlanRepository(PlanthorDbContext context) : BaseRepository<Plan>(context), IPlanRepository
{
    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Plans
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
