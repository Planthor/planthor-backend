using System;
using Domain.Plans;
using NodaTime;
using Xunit;

namespace Domain.Tests.Plans;

public sealed class ExternalActivityPlanPolicyTests
{
    private static readonly Instant PeriodStart = Instant.FromUtc(2026, 1, 1, 0, 0);
    private static readonly Instant PeriodEnd = Instant.FromUtc(2026, 1, 31, 23, 59);
    private static readonly IClock Clock = new TestClock(PeriodStart);

    [Fact]
    public void IsEligible_WithNullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        Plan? plan = null;

        // Act
        void Action() => ExternalActivityPlanPolicy.IsEligible(plan!, true);

        // Assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void IsEligible_WithActiveLinkedSportPlan_ReturnsTrue()
    {
        // Arrange
        var plan = CreateSportPlan();
        plan.Activate(Guid.NewGuid(), Clock);

        // Act
        var eligible = ExternalActivityPlanPolicy.IsEligible(plan, true);

        // Assert
        Assert.True(eligible);
    }

    [Fact]
    public void IsEligible_WithUnlinkedPlan_ReturnsFalse()
    {
        // Arrange
        var plan = CreateSportPlan();
        plan.Activate(Guid.NewGuid(), Clock);

        // Act
        var eligible = ExternalActivityPlanPolicy.IsEligible(plan, false);

        // Assert
        Assert.False(eligible);
    }

    [Fact]
    public void IsEligible_WithPlannedPlan_ReturnsFalse()
    {
        // Arrange
        var plan = CreateSportPlan();

        // Act
        var eligible = ExternalActivityPlanPolicy.IsEligible(plan, true);

        // Assert
        Assert.False(eligible);
    }

    [Fact]
    public void IsEligible_WithActivityLoggingDisabled_ReturnsFalse()
    {
        // Arrange
        var plan = CreateSportPlan(enableActivityLog: false);
        plan.Activate(Guid.NewGuid(), Clock);

        // Act
        var eligible = ExternalActivityPlanPolicy.IsEligible(plan, true);

        // Assert
        Assert.False(eligible);
    }

    [Fact]
    public void IsEligible_WithGenericPlan_ReturnsFalse()
    {
        // Arrange
        var plan = CreateGenericPlan();
        plan.Activate(Guid.NewGuid(), Clock);

        // Act
        var eligible = ExternalActivityPlanPolicy.IsEligible(plan, true);

        // Assert
        Assert.False(eligible);
    }

    [Fact]
    public void TryMatch_WithNullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        Plan? plan = null;

        // Act
        void Action() => ExternalActivityPlanPolicy.TryMatch(
                plan!,
                PlanthorSportType.Run.Id,
                PeriodStart,
                PeriodEnd,
                out _);

        // Assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void TryMatch_WithActivityAfterRunUpperBound_ReturnsFalse()
    {
        // Arrange
        var plan = CreateSportPlan();

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            PlanthorSportType.Run.Id,
            PeriodEnd.Plus(Duration.FromSeconds(1)),
            PeriodEnd,
            out var activityLocalDate);

        // Assert
        Assert.False(matches);
        Assert.Empty(activityLocalDate);
    }

    [Fact]
    public void TryMatch_WithEmptySportType_ReturnsFalse()
    {
        // Arrange
        var plan = CreateSportPlan();

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            " ",
            PeriodStart,
            PeriodEnd,
            out var activityLocalDate);

        // Assert
        Assert.False(matches);
        Assert.Empty(activityLocalDate);
    }

    [Fact]
    public void TryMatch_WithGenericPlan_ReturnsFalse()
    {
        // Arrange
        var plan = CreateGenericPlan();

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            PlanthorSportType.Run.Id,
            PeriodStart,
            PeriodEnd,
            out _);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void TryMatch_WithDifferentSportType_ReturnsFalse()
    {
        // Arrange
        var plan = CreateSportPlan([PlanthorSportType.Ride.Id]);

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            PlanthorSportType.Run.Id,
            PeriodStart,
            PeriodEnd,
            out _);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void TryMatch_WithWildcardSportType_ReturnsTrue()
    {
        // Arrange
        var plan = CreateSportPlan([PlanthorSportType.All.Id]);
        var occurredAt = Instant.FromUtc(2026, 1, 15, 12, 0);

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            PlanthorSportType.Run.Id,
            occurredAt,
            PeriodEnd,
            out var activityLocalDate);

        // Assert
        Assert.True(matches);
        Assert.Equal("2026-01-15", activityLocalDate);
    }

    [Fact]
    public void TryMatch_WithMatchingSportTypeIgnoringCase_ReturnsTrue()
    {
        // Arrange
        var plan = CreateSportPlan(["run"]);
        var occurredAt = Instant.FromUtc(2026, 1, 15, 12, 0);

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            "RUN",
            occurredAt,
            PeriodEnd,
            out _);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void TryMatch_WithUnknownTimezone_ReturnsFalse()
    {
        // Arrange
        var plan = CreateSportPlan();
        typeof(Plan).GetProperty(nameof(Plan.Timezone))!.SetValue(plan, "Unknown/Timezone");

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            PlanthorSportType.Run.Id,
            PeriodStart,
            PeriodEnd,
            out _);

        // Assert
        Assert.False(matches);
    }

    [Theory]
    [InlineData(2025, 12, 31)]
    [InlineData(2026, 2, 1)]
    public void TryMatch_WithActivityOutsideLocalDateRange_ReturnsFalse(
        int year,
        int month,
        int day)
    {
        // Arrange
        var plan = CreateSportPlan();
        var occurredAt = Instant.FromUtc(year, month, day, 12, 0);

        // Act
        var matches = ExternalActivityPlanPolicy.TryMatch(
            plan,
            PlanthorSportType.Run.Id,
            occurredAt,
            Instant.FromUtc(2026, 3, 1, 0, 0),
            out _);

        // Assert
        Assert.False(matches);
    }

    private static Plan CreateSportPlan(
        string[]? sportTypes = null,
        bool enableActivityLog = true) =>
        Plan.CreateSportPlan(
            "January activity",
            "km",
            100,
            PeriodStart,
            PeriodEnd,
            "2026-01-01",
            "2026-01-31",
            "UTC",
            enableActivityLog,
            new SportPlanDetails("km", sportTypes ?? [PlanthorSportType.Run.Id]),
            Clock,
            Guid.NewGuid());

    private static Plan CreateGenericPlan() =>
        Plan.Create(
            "January activity",
            "km",
            100,
            PeriodStart,
            PeriodEnd,
            "2026-01-01",
            "2026-01-31",
            "UTC",
            true,
            Clock,
            Guid.NewGuid());

    private sealed class TestClock(Instant current) : IClock
    {
        public Instant GetCurrentInstant() => current;
    }
}
