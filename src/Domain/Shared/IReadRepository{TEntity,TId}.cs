using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Shared;

/// <summary>
/// Defines a read-only repository for querying entities.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type being queried. Does not need to be an aggregate root —
/// projections and read models are valid here.
/// </typeparam>
/// <typeparam name="TId">
/// The strongly-typed identifier of the entity.
/// </typeparam>
public interface IReadRepository<TEntity, TId>
    where TEntity : IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity, or null if not found.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
