using Domain.Plans;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for managing <see cref="Plan"/> entities, providing data access operations specifically for user plans.
/// </summary>
public class PlanRepository(PlanthorDbContext context) : BaseRepository<Plan>(context), IPlanRepository
{
    /// <inheritdoc />
    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Plans
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
