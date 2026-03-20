using System;
using Microsoft.EntityFrameworkCore;
using PlanthorWebApi.Domain;

namespace PlanthorWebApi.Infrastructure;

/// <summary>
/// Represents the database context for the Planthor application.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class PlanthorDbContext(DbContextOptions options) : DbContext(options)
{
    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanthorDbContext).Assembly);
    }
}
