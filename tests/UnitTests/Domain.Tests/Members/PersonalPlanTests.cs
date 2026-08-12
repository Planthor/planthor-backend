using System;
using System.Reflection;
using Domain.Members;
using NodaTime;

using Xunit;

namespace Domain.Tests.Members;

public class PersonalPlanTests
{
    private class DummyClock : IClock
    {
        public Instant GetCurrentInstant() => Instant.FromUtc(2024, 1, 1, 0, 0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Create_InvalidPriority_ThrowsArgumentOutOfRangeException(int priority)
    {
        var method = typeof(PersonalPlan).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static);
        var targetInvocationEx = Assert.Throws<TargetInvocationException>(() => 
            method!.Invoke(null, [Guid.NewGuid(), Guid.NewGuid(), true, priority, true, new DummyClock()]));
        Assert.IsType<ArgumentOutOfRangeException>(targetInvocationEx.InnerException);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public void UpdatePreferences_InvalidPriority_ThrowsArgumentOutOfRangeException(int priority)
    {
        var createMethod = typeof(PersonalPlan).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static);
        // Valid priority is 0-999
        var personalPlan = createMethod!.Invoke(null, [Guid.NewGuid(), Guid.NewGuid(), true, 10, true, new DummyClock()]);

        var updateMethod = typeof(PersonalPlan).GetMethod("UpdatePreferences", BindingFlags.NonPublic | BindingFlags.Instance);
        var targetInvocationEx = Assert.Throws<TargetInvocationException>(() => 
            updateMethod!.Invoke(personalPlan, [true, priority, true, new DummyClock()]));
        Assert.IsType<ArgumentOutOfRangeException>(targetInvocationEx.InnerException);
    }
}
