using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Shared;
using Moq;
using Xunit;

namespace Application.Tests.Shared;

public class DomainEventNotificationHandlerTests
{
    private readonly Mock<IDomainEventHandler<IDomainEvent>> _mockHandler;
    private readonly DomainEventNotificationHandler<IDomainEvent> _sut;

    public DomainEventNotificationHandlerTests()
    {
        _mockHandler = new Mock<IDomainEventHandler<IDomainEvent>>();
        _sut = new DomainEventNotificationHandler<IDomainEvent>([_mockHandler.Object]);
    }

    [Fact]
    public async Task Handle_CallsAllRegisteredHandlers()
    {
        var domainEvent = new Mock<IDomainEvent>();
        var notification = new DomainEventNotification<IDomainEvent>(domainEvent.Object);

        await _sut.Handle(notification, CancellationToken.None);

        _mockHandler.Verify(h => h.HandleAsync(domainEvent.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullNotification_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MultipleHandlers_CallsEachHandler()
    {
        var handler2 = new Mock<IDomainEventHandler<IDomainEvent>>();
        var multi = new DomainEventNotificationHandler<IDomainEvent>(
            [_mockHandler.Object, handler2.Object]);

        var domainEvent = new Mock<IDomainEvent>();
        var notification = new DomainEventNotification<IDomainEvent>(domainEvent.Object);

        await multi.Handle(notification, CancellationToken.None);

        _mockHandler.Verify(h => h.HandleAsync(domainEvent.Object, It.IsAny<CancellationToken>()), Times.Once);
        handler2.Verify(h => h.HandleAsync(domainEvent.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoHandlers_CompletesWithoutError()
    {
        var empty = new DomainEventNotificationHandler<IDomainEvent>(new List<IDomainEventHandler<IDomainEvent>>());
        var notification = new DomainEventNotification<IDomainEvent>(new Mock<IDomainEvent>().Object);

        await empty.Handle(notification, CancellationToken.None);
    }
}
