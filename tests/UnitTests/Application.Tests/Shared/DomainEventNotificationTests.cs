using System;
using Application.Shared;
using Domain.Shared;
using NSubstitute;

namespace Application.Tests.Shared;

public class DomainEventNotificationTests
{
    [Fact]
    public void Constructor_SetsDomainEvent()
    {
        var domainEvent = Substitute.For<IDomainEvent>();
        domainEvent.EventId.Returns(Guid.NewGuid());

        var notification = new DomainEventNotification<IDomainEvent>(domainEvent);

        Assert.Equal(domainEvent, notification.DomainEvent);
    }

    [Fact]
    public void DomainEvent_IsAccessibleAfterConstruction()
    {
        var domainEvent = Substitute.For<IDomainEvent>();
        var id = Guid.NewGuid();
        domainEvent.EventId.Returns(id);

        var notification = new DomainEventNotification<IDomainEvent>(domainEvent);

        Assert.Equal(id, notification.DomainEvent.EventId);
    }
}
