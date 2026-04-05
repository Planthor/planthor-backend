using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

/// <summary>
/// Represents the database context for the Planthor application.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class PlanthorDbContext(DbContextOptions options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanthorDbContext).Assembly);
    }

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        return CoreSaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task<int> CoreSaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        // STAGE 1: Collect events from all entities BEFORE they are cleared
        var domainEvents = ChangeTracker.Entries<IAggregateRoot>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        // (We must also ensure they are cleared so they don't dispatch twice in the same context instance)
        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        // STAGE 2: Publish collected events only AFTER a successful commit
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }

        return result;
    }
}
