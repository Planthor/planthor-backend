using System;
using Domain.Plans;
using Domain.Shared.Exceptions;
using NodaTime;
using Xunit;

namespace Domain.Tests.Plans;

public class PlanTests
{
    private sealed class TestClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }

    private static readonly IClock Clock = new TestClock(Instant.FromUtc(2026, 1, 1, 0, 0));
    private static readonly Instant From = Instant.FromUtc(2026, 1, 1, 0, 0);
    private static readonly Instant To = Instant.FromUtc(2026, 12, 31, 23, 59);

    private static Plan CreateValid(
        string name = "Run 100km",
        string unit = "km",
        float target = 100f,
        string startDate = "2026-01-01",
        string endDate = "2026-12-31") =>
        Plan.Create(name, unit, target, From, To, startDate, endDate, "UTC", true, Clock, Guid.NewGuid());

    [Fact]
    public void Create_ValidInput_ReturnsNewPlan()
    {
        var plan = CreateValid();

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal("Run 100km", plan.Name);
        Assert.Equal("km", plan.Unit);
        Assert.Equal(100f, plan.Target);
        Assert.Equal(PlanStatus.Planned, plan.Status);
        Assert.Equal(0f, plan.CurrentValue);
        Assert.False(plan.Completed);
        Assert.Equal(0, plan.LikeCount);
        Assert.Null(plan.SportPlanDetails);
        Assert.Empty(plan.ActivityLogs);
    }

    [Fact]
    public void Create_RaisesPlanCreatedEvent()
    {
        var plan = CreateValid();

        Assert.Single(plan.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsDomainValidationException(string name)
    {
        Assert.Throws<DomainValidationException>(() => CreateValid(name: name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyUnit_ThrowsDomainValidationException(string unit)
    {
        Assert.Throws<DomainValidationException>(() => CreateValid(unit: unit));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void Create_TargetNotPositive_ThrowsDomainValidationException(float target)
    {
        Assert.Throws<DomainValidationException>(() => CreateValid(target: target));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyStartDate_ThrowsDomainValidationException(string startDate)
    {
        Assert.Throws<DomainValidationException>(() => CreateValid(startDate: startDate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyEndDate_ThrowsDomainValidationException(string endDate)
    {
        Assert.Throws<DomainValidationException>(() => CreateValid(endDate: endDate));
    }

    [Fact]
    public void Create_StartDateAfterEndDate_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            Plan.Create("Run", "km", 100f, From, To, "2026-12-31", "2026-01-01", "UTC", true, Clock, Guid.NewGuid()));
    }

    [Fact]
    public void Create_StartDateEqualsEndDate_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            Plan.Create("Run", "km", 100f, From, To, "2026-01-01", "2026-01-01", "UTC", true, Clock, Guid.NewGuid()));
    }

    [Fact]
    public void Create_PeriodFromEqualsTo_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            Plan.Create("Run", "km", 100f, From, From, "2026-01-01", "2026-12-31", "UTC", true, Clock, Guid.NewGuid()));
    }

    [Fact]
    public void Create_PeriodFromAfterTo_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            Plan.Create("Run", "km", 100f, To, From, "2026-01-01", "2026-12-31", "UTC", true, Clock, Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyTimezone_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            Plan.Create("Run", "km", 100f, From, To, "2026-01-01", "2026-12-31", "", true, Clock, Guid.NewGuid()));
    }

    [Fact]
    public void CreateSportPlan_ValidInput_SetsSportPlanDetails()
    {
        var sportDetails = new SportPlanDetails("km", ["Run"]);

        var plan = Plan.CreateSportPlan(
            "Run 100km", "km", 100f, From, To, "2026-01-01", "2026-12-31",
            "UTC", true, sportDetails, Clock, Guid.NewGuid());

        Assert.NotNull(plan.SportPlanDetails);
        Assert.Equal("km", plan.SportPlanDetails.Unit);
        Assert.Contains("Run", plan.SportPlanDetails.SportTypes);
    }

    [Fact]
    public void CreateSportPlan_NullSportDetails_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Plan.CreateSportPlan(
                "Run 100km", "km", 100f, From, To, "2026-01-01", "2026-12-31",
                "UTC", true, null!, Clock, Guid.NewGuid()));
    }

    [Fact]
    public void CreateSportPlan_RaisesPlanCreatedEvent()
    {
        var plan = Plan.CreateSportPlan(
            "Run 100km", "km", 100f, From, To, "2026-01-01", "2026-12-31",
            "UTC", true, new SportPlanDetails(), Clock, Guid.NewGuid());

        Assert.Single(plan.DomainEvents);
    }

    [Fact]
    public void Create_StampsCreatedAudit()
    {
        var userId = Guid.NewGuid();
        var plan = Plan.Create("Run", "km", 100f, From, To, "2026-01-01", "2026-12-31", "UTC", true, Clock, userId);

        Assert.Equal(userId, plan.CreatedBy);
        Assert.Equal(userId, plan.LastUpdatedBy);
        Assert.Equal(Clock.GetCurrentInstant(), plan.CreatedAt);
    }
}
