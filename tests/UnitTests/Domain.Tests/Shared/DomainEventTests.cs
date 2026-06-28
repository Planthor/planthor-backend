using System;
using Domain.Plans.Events;
using NodaTime;
using Xunit;

namespace Domain.Tests.Shared;

public class DomainEventTests
{
    private sealed class TestClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }

    private static readonly IClock Clock = new TestClock(Instant.FromUtc(2026, 1, 1, 0, 0));

    [Fact]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PlanCreatedEvent(Guid.NewGuid(), "Run", 100f, "km", "2026-01-01", "2026-12-31", "UTC", null!, "test"));
    }

    [Fact]
    public void Constructor_ValidClock_SetsProperties()
    {
        var planId = Guid.NewGuid();

        var evt = new PlanCreatedEvent(planId, "Run", 100f, "km", "2026-01-01", "2026-12-31", "UTC", Clock, "Member/Create");

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(Clock.GetCurrentInstant(), evt.OccurredAt);
        Assert.Equal("Member/Create", evt.OccurredBy);
    }
}
