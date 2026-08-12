using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Shared;
using NodaTime;
using NSubstitute;

namespace Application.Tests.Shared;

public class DomainEventNotificationHandlerTests
{
    private readonly IDomainEventHandler<IDomainEvent> _mockHandler;
    private readonly DomainEventNotificationHandler<IDomainEvent> _sut;

    public DomainEventNotificationHandlerTests()
    {
        _mockHandler = Substitute.For<IDomainEventHandler<IDomainEvent>>();
        _sut = new DomainEventNotificationHandler<IDomainEvent>([_mockHandler]);
    }

    [Fact]
    public async Task Handle_CallsAllRegisteredHandlers()
    {
        var domainEvent = Substitute.For<IDomainEvent>();
        var notification = new DomainEventNotification<IDomainEvent>(domainEvent);

        await _sut.Handle(notification, CancellationToken.None);

        await _mockHandler.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
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
        var handler2 = Substitute.For<IDomainEventHandler<IDomainEvent>>();
        var multi = new DomainEventNotificationHandler<IDomainEvent>(
            [_mockHandler, handler2]);

        var domainEvent = Substitute.For<IDomainEvent>();
        var notification = new DomainEventNotification<IDomainEvent>(domainEvent);

        await multi.Handle(notification, CancellationToken.None);

        await _mockHandler.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
        await handler2.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoHandlers_CompletesWithoutError()
    {
        var empty = new DomainEventNotificationHandler<IDomainEvent>([]);
        var notification = new DomainEventNotification<IDomainEvent>(Substitute.For<IDomainEvent>());

        var exception = await Record.ExceptionAsync(() => empty.Handle(notification, CancellationToken.None));

        Assert.Null(exception);
    }

    public class TestEvent : IDomainEvent
    {
        public Guid EventId => Guid.NewGuid();
        public Instant OccurredAt => SystemClock.Instance.GetCurrentInstant();
        public string Source => "Test";
        public string OccurredBy => "System";
    }

    [Fact]
    public async Task Handle_WithConcreteEvent_CallsAllRegisteredHandlers()
    {
        var mockHandler = Substitute.For<IDomainEventHandler<TestEvent>>();
        var sut = new DomainEventNotificationHandler<TestEvent>([mockHandler]);
        
        var domainEvent = new TestEvent();
        var notification = new DomainEventNotification<TestEvent>(domainEvent);

        await sut.Handle(notification, CancellationToken.None);

        await mockHandler.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
    }
}
