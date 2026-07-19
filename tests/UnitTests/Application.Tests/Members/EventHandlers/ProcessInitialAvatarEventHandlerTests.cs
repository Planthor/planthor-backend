using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.EventHandler;
using Domain.Members.Events;
using NSubstitute;
using NodaTime;

namespace Application.Tests.Members.EventHandlers;

public class ProcessInitialAvatarEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_NotYetImplemented_ThrowsNotImplementedException()
    {
        var handler = new ProcessInitialAvatarEventHandler();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 1, 1, 0, 0));
        var memberId = Guid.NewGuid();
        var evt = new MemberRegisteredEvent(memberId, null, clock, "test");

        await Assert.ThrowsAsync<NotImplementedException>(() =>
            handler.HandleAsync(evt, CancellationToken.None));
    }
}
