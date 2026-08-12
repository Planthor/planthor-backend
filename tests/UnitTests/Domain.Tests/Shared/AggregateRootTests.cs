using System;
using Domain.Shared;
using NodaTime;

using Xunit;

namespace Domain.Tests.Shared;

public class AggregateRootTests
{
    private class TestAggregate : AggregateRoot<Guid>
    {
        public override ValidationResult Validate() => new ValidationResult([]);

        public void TestRaiseEvent(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }

        public void TestStampCreated(Guid userId, IClock clock)
        {
            StampCreatedAudit(userId, clock);
        }

        public void TestStampUpdated(Guid userId, IClock clock)
        {
            StampUpdatedAudit(userId, clock);
        }
    }

    [Fact]
    public void RaiseDomainEvent_NullEvent_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate();
        Assert.Throws<ArgumentNullException>(() => aggregate.TestRaiseEvent(null!));
    }

    [Fact]
    public void StampCreatedAudit_NullClock_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate();
        Assert.Throws<ArgumentNullException>(() => aggregate.TestStampCreated(Guid.NewGuid(), null!));
    }

    [Fact]
    public void StampUpdatedAudit_NullClock_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate();
        Assert.Throws<ArgumentNullException>(() => aggregate.TestStampUpdated(Guid.NewGuid(), null!));
    }
}
