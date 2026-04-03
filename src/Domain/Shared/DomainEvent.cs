using System;
using NodaTime;

namespace Domain.Shared;

/// <summary>
/// Abstract base implementation of <see cref="IDomainEvent"/>.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of <see cref="DomainEvent"/>,
    /// capturing the current UTC instant from the provided clock.
    /// </summary>
    /// <param name="clock">
    /// The system clock abstraction providing the current UTC instant.
    /// Injected by the aggregate root that raises the event — never
    /// resolved internally — so that time remains controllable in tests
    /// and time-travel scenarios.
    /// </param>
    protected DomainEvent(IClock clock, string occurredBy)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        EventId = Guid.NewGuid();
        OccurredAt = clock.GetCurrentInstant();
        OccurredBy = occurredBy;
    }

    /// <summary>
    /// Gets the unique identifier of this domain event instance.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Gets the exact UTC instant at which this domain event occurred.
    /// </summary>
    public Instant OccurredAt { get; }

    /// <summary>
    /// Gets the name or identifier component that make the event occurred.
    /// </summary>
    public string OccurredBy { get; }
}
