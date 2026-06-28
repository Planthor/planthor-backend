using System;
using System.Collections.Generic;
using Domain.Plans;
using Xunit;

namespace Domain.Tests.Plans;

public class SportPlanDetailsTests
{
    [Fact]
    public void DefaultConstructor_SetsKmAndEmptySportTypes()
    {
        var details = new SportPlanDetails();

        Assert.Equal("km", details.Unit);
        Assert.Empty(details.SportTypes);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var sportTypes = new List<string> { "Run", "Ride" };
        var details = new SportPlanDetails("m", sportTypes.AsReadOnly());

        Assert.Equal("m", details.Unit);
        Assert.Equal(2, details.SportTypes.Count);
        Assert.Contains("Run", details.SportTypes);
    }

    [Fact]
    public void ParameterizedConstructor_EmptyUnit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SportPlanDetails("", []));
    }

    [Fact]
    public void ParameterizedConstructor_WhitespaceUnit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SportPlanDetails("   ", []));
    }

    [Fact]
    public void ParameterizedConstructor_NullSportTypes_DefaultsToEmpty()
    {
        var details = new SportPlanDetails("km", null!);

        Assert.NotNull(details.SportTypes);
        Assert.Empty(details.SportTypes);
    }

    [Fact]
    public void SameUnitAndSportTypes_AreEqual()
    {
        var a = new SportPlanDetails("km", ["Run"]);
        var b = new SportPlanDetails("km", ["Run"]);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void DifferentUnit_AreNotEqual()
    {
        var a = new SportPlanDetails("km", ["Run"]);
        var b = new SportPlanDetails("m", ["Run"]);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void DifferentSportTypes_AreNotEqual()
    {
        var a = new SportPlanDetails("km", ["Run"]);
        var b = new SportPlanDetails("km", ["Ride"]);

        Assert.NotEqual(a, b);
    }
}
