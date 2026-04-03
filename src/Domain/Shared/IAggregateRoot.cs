using System.Collections.Generic;

namespace Domain.Shared;

/// <summary>
/// Represents an aggregate root in the domain model.
/// </summary>
/// <remarks>
/// In Domain-Driven Design (DDD), an aggregate root is an entity that acts as a boundary for a group of related entities.
/// It is responsible for maintaining the consistency and integrity of the entities within the aggregate.
/// </remarks>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the domain events raised by this aggregate root during
    /// the current operation.
    /// </summary>
    /// <remarks>
    /// Domain events are accumulated during aggregate state changes and
    /// dispatched <b>after</b> the aggregate is successfully persisted,
    /// ensuring that side effects only trigger once the transaction commits.
    /// <para>
    /// Consumers should call <see cref="ClearDomainEvents"/> after
    /// dispatching to prevent duplicate event processing.
    /// </para>
    /// </remarks>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events that have been raised by this aggregate root.
    /// </summary>
    /// <remarks>
    /// Must be called by the infrastructure layer — typically inside the
    /// repository or Unit of Work — immediately after all raised events
    /// have been successfully dispatched. Failing to call this method
    /// will result in duplicate event dispatching on subsequent operations.
    /// </remarks>
    void ClearDomainEvents();

    /// <summary>
    /// Validates the aggregate root against its business invariants.
    /// </summary>
    /// <returns>
    /// A <see cref="ValidationResult"/> containing any <see cref="ValidationError"/>
    /// instances found. Check <see cref="ValidationResult.IsValid"/> to determine
    /// whether the aggregate is in a consistent state.
    /// </returns>
    /// <remarks>
    /// Validation should be called inside the aggregate's static factory method
    /// before the instance is returned to the caller — never after. This ensures
    /// an aggregate can never exist in an invalid state.
    /// </remarks>
    ValidationResult Validate();
}
