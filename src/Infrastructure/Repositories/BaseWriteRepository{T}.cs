using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

/// <summary>
/// A generic base repository implementation providing common write operations (Add, Update, Delete) for aggregate roots.
/// </summary>
public abstract class BaseRepository<T> : IWriteRepository<T> where T : class, IAggregateRoot
{
    protected readonly PlanthorDbContext Context;

    protected BaseRepository(PlanthorDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T item, CancellationToken cancellationToken)
    {
        await Context.Set<T>().AddAsync(item, cancellationToken);
        return item;
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(T item, CancellationToken cancellationToken)
    {
        Context.Set<T>().Remove(item);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(T item, CancellationToken cancellationToken)
    {
        Context.Set<T>().Update(item);
        return Task.CompletedTask;
    }
}
