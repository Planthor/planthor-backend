using Domain.Shared;
using MediatR;

namespace Application.Shared;

// This wrapper tells MediatR how to carry your pure Domain Event
/// <summary>
/// A wrapper that adapts a pure domain event (<see cref="IDomainEvent"/>) into a MediatR <see cref="INotification"/>.
/// This allows domain events to be dispatched using MediatR's publish-subscribe mechanism across the application layer.
/// </summary>
public class DomainEventNotification<TEvent>(TEvent domainEvent) : INotification where TEvent : IDomainEvent
{
    /// <summary>
    /// Gets the underlying domain event that triggered this notification.
    /// </summary>
    public TEvent DomainEvent { get; } = domainEvent;
}
