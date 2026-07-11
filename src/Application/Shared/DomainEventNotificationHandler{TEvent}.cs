using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared;
using MediatR;

namespace Application.Shared;

/// <summary>
/// A MediatR notification handler that wraps and dispatches domain events to their respective <see cref="IDomainEventHandler{TEvent}"/> implementations.
/// </summary>
public class DomainEventNotificationHandler<TEvent>(
    IEnumerable<IDomainEventHandler<TEvent>> handlers) : INotificationHandler<DomainEventNotification<TEvent>>
    where TEvent : IDomainEvent
{
    // Inject all handlers registered for this specific event type
    private readonly IEnumerable<IDomainEventHandler<TEvent>> _handlers = handlers;

    /// <inheritdoc />
    public Task Handle(DomainEventNotification<TEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return HandleAsync(notification, cancellationToken);
    }

    private async Task HandleAsync(DomainEventNotification<TEvent> notification, CancellationToken cancellationToken)
    {
        foreach (var handler in _handlers)
        {
            await handler.HandleAsync(notification.DomainEvent, cancellationToken);
        }
    }
}

