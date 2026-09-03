using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Plans;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Plan"/> entities, providing data access operations specifically for user plans.
/// </summary>
public sealed class PlanRepository(PlanthorDbContext context) : BaseRepository<Plan>(context), IPlanRepository
{
    /// <inheritdoc />
    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Plans
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Plan>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Plan>>([]);
        }

        return GetByIdsCoreAsync(ids, cancellationToken);
    }

    private async Task<IReadOnlyList<Plan>> GetByIdsCoreAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        return await Context.Plans
            .Where(plan => ids.Contains(plan.Id))
            .ToListAsync(cancellationToken);
    }
}
