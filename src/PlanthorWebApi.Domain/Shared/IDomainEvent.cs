using System;
using NodaTime;

namespace PlanthorWebApi.Domain.Shared;

/// <summary>
/// Marker interface for all domain events in the system.
/// </summary>
/// <remarks>
/// In Domain-Driven Design (DDD), a domain event represents something meaningful
/// that happened within the domain. Aggregates raise domain events to communicate
/// side effects to other parts of the system in a decoupled way.
/// All domain events must implement this interface.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of this domain event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp of when this domain event occurred.
    /// </summary>
    Instant OccurredAt { get; }
}
