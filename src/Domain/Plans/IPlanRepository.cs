using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared;

namespace Domain.Plans;

/// <summary>
/// Repository interface for Plan entities.
/// </summary>
public interface IPlanRepository : IWriteRepository<Plan>
{
    /// <summary>
    /// Gets a plan by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the plan.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the plan, or null if not found.</returns>
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets plans by their aggregate identifiers in a single repository operation.
    /// </summary>
    /// <param name="ids">The plan identifiers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plans that currently exist.</returns>
    Task<IReadOnlyList<Plan>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
}
