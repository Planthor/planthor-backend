using System;
using System.Collections.Generic;
using NodaTime;

namespace Domain.Shared;

/// <summary>
/// Abstract base class for all aggregate roots in the domain model.
/// </summary>
/// <typeparam name="TId">
/// The type of the aggregate root's unique identifier.
/// </typeparam>
public abstract class AggregateRoot<TId> : IAggregateRoot, IEntity<TId>, IHasAudit
    where TId : notnull
{

    /// <inheritdoc/>
    public TId Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc/>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event, queuing it for dispatch after the
    /// aggregate is successfully persisted.
    /// </summary>
    /// <param name="domainEvent">
    /// The domain event to raise. Must not be null.
    /// </param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <inheritdoc/>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <inheritdoc/>
    public Instant CreatedAt { get; protected set; }

    /// <inheritdoc/>
    public Guid CreatedBy { get; protected set; }

    /// <inheritdoc/>
    public Instant LastUpdatedAt { get; protected set; }

    /// <inheritdoc/>
    public Guid LastUpdatedBy { get; protected set; }

    /// <summary>
    /// Stamps the audit fields on initial creation.
    /// Called once inside the aggregate's static factory method.
    /// </summary>
    /// <param name="byUserId">The identifier of the user creating this aggregate.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    protected void StampCreatedAudit(Guid byUserId, IClock clock)
    {
        CreatedAt = clock.GetCurrentInstant();
        CreatedBy = byUserId;
        LastUpdatedAt = clock.GetCurrentInstant();
        LastUpdatedBy = byUserId;
    }

    /// <summary>
    /// Stamps the audit fields on every subsequent modification.
    /// Called inside domain methods that mutate aggregate state.
    /// </summary>
    /// <param name="byUserId">The identifier of the user performing the modification.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    protected void StampUpdatedAudit(Guid byUserId, IClock clock)
    {
        LastUpdatedAt = clock.GetCurrentInstant();
        LastUpdatedBy = byUserId;
    }

    /// <summary>
    /// Validates the aggregate root against its business invariants.
    /// </summary>
    /// <returns>
    /// A <see cref="ValidationResult"/> describing whether the aggregate
    /// is in a consistent, valid state.
    /// </returns>
    public abstract ValidationResult Validate();
}
