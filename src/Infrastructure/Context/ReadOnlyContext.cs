using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

/// <summary>
/// Implements read-only operations using EF Core, optimized for CQRS read queries.
/// </summary>
/// <param name="context">The database context.</param>
public class ReadOnlyContext(PlanthorDbContext context) : IReadOnlyContext
{
    private readonly PlanthorDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task<List<TResult>> QueryAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken) where TEntity : class
    {
        var query = _context.Set<TEntity>().AsNoTracking();
        return await queryBuilder(query).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult?> FirstOrDefaultAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken) where TEntity : class
    {
        var query = _context.Set<TEntity>().AsNoTracking();
        return await queryBuilder(query).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync<TEntity>(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken) where TEntity : class
    {
        var query = _context.Set<TEntity>().AsNoTracking();
        return await queryBuilder(query).AnyAsync(cancellationToken);
    }
}
