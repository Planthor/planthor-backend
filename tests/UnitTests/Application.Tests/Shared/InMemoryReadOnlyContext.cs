using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;

namespace Application.Tests.Shared;

internal sealed class InMemoryReadOnlyContext : IReadOnlyContext
{
    private readonly Dictionary<Type, IQueryable> _sets = [];

    internal CancellationToken LastCancellationToken { get; private set; }

    internal void SetEntities<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        _sets[typeof(TEntity)] = entities.AsQueryable();
    }

    public Task<List<TResult>> QueryAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(queryBuilder);
        LastCancellationToken = cancellationToken;

        return Task.FromResult(queryBuilder(GetQuery<TEntity>()).ToList());
    }

    public Task<TResult?> FirstOrDefaultAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(queryBuilder);
        LastCancellationToken = cancellationToken;

        return Task.FromResult(queryBuilder(GetQuery<TEntity>()).FirstOrDefault());
    }

    public Task<bool> AnyAsync<TEntity>(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(queryBuilder);
        LastCancellationToken = cancellationToken;

        return Task.FromResult(queryBuilder(GetQuery<TEntity>()).Any());
    }

    private IQueryable<TEntity> GetQuery<TEntity>()
        where TEntity : class
    {
        return _sets.TryGetValue(typeof(TEntity), out var query)
            ? (IQueryable<TEntity>)query
            : Enumerable.Empty<TEntity>().AsQueryable();
    }
}
