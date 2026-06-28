using System;
using Application.Shared;
using Domain.Shared;
using Moq;
using Xunit;

namespace Application.Tests.Shared;

public class DomainEventNotificationTests
{
    [Fact]
    public void Constructor_SetsDomainEvent()
    {
        var domainEvent = new Mock<IDomainEvent>();
        domainEvent.Setup(e => e.EventId).Returns(Guid.NewGuid());

        var notification = new DomainEventNotification<IDomainEvent>(domainEvent.Object);

        Assert.Equal(domainEvent.Object, notification.DomainEvent);
    }

    [Fact]
    public void DomainEvent_IsAccessibleAfterConstruction()
    {
        var domainEvent = new Mock<IDomainEvent>();
        var id = Guid.NewGuid();
        domainEvent.Setup(e => e.EventId).Returns(id);

        var notification = new DomainEventNotification<IDomainEvent>(domainEvent.Object);

        Assert.Equal(id, notification.DomainEvent.EventId);
    }
}
