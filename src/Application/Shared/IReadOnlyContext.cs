using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared;

/// <summary>
/// Provides a generic, EF-agnostic mechanism to query domain entities for read-only operations.
/// </summary>
public interface IReadOnlyContext
{
    /// <summary>
    /// Executes a query with a cancellation token and returns a list of projected results.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity.</typeparam>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="queryBuilder">A function to build the query from the entity queryable.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, returning a list of projected results.</returns>
    Task<List<TResult>> QueryAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken) where TEntity : class;

    /// <summary>
    /// Executes a query with a cancellation token and returns the first projected result or null.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity.</typeparam>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="queryBuilder">A function to build the query from the entity queryable.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, returning the first projected result or null.</returns>
    Task<TResult?> FirstOrDefaultAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken) where TEntity : class;

    /// <summary>
    /// Checks with a cancellation token if any elements satisfy the defined query.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity.</typeparam>
    /// <param name="queryBuilder">A function to build the query from the entity queryable.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if any elements satisfy the query; otherwise, false.</returns>
    Task<bool> AnyAsync<TEntity>(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken) where TEntity : class;
}
