using System;
using Domain.Plans;
using Xunit;

namespace Domain.Tests.Plans;

public class PlanStatusTests
{
    [Theory]
    [InlineData("P")]
    [InlineData("p")]
    public void FromId_Planned_ReturnsPlanned(string id)
    {
        Assert.Equal(PlanStatus.Planned, PlanStatus.FromId(id));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("a")]
    public void FromId_Active_ReturnsActive(string id)
    {
        Assert.Equal(PlanStatus.Active, PlanStatus.FromId(id));
    }

    [Theory]
    [InlineData("E")]
    [InlineData("e")]
    public void FromId_Exceeded_ReturnsExceeded(string id)
    {
        Assert.Equal(PlanStatus.Exceeded, PlanStatus.FromId(id));
    }

    [Theory]
    [InlineData("C")]
    [InlineData("c")]
    public void FromId_Closed_ReturnsClosed(string id)
    {
        Assert.Equal(PlanStatus.Closed, PlanStatus.FromId(id));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("")]
    [InlineData("INVALID")]
    public void FromId_InvalidId_ThrowsArgumentException(string id)
    {
        Assert.Throws<ArgumentException>(() => PlanStatus.FromId(id));
    }

    [Fact]
    public void All_ContainsFourStatuses()
    {
        Assert.Equal(4, PlanStatus.All.Count);
    }

    [Fact]
    public void All_ContainsAllExpectedStatuses()
    {
        Assert.Contains(PlanStatus.Planned, PlanStatus.All);
        Assert.Contains(PlanStatus.Active, PlanStatus.All);
        Assert.Contains(PlanStatus.Exceeded, PlanStatus.All);
        Assert.Contains(PlanStatus.Closed, PlanStatus.All);
    }

    [Fact]
    public void Planned_HasCorrectProperties()
    {
        Assert.Equal("P", PlanStatus.Planned.Id);
        Assert.Equal("PLANNED", PlanStatus.Planned.Name);
    }

    [Fact]
    public void Active_HasCorrectProperties()
    {
        Assert.Equal("A", PlanStatus.Active.Id);
    }

    [Fact]
    public void Exceeded_HasCorrectProperties()
    {
        Assert.Equal("E", PlanStatus.Exceeded.Id);
    }

    [Fact]
    public void Closed_HasCorrectProperties()
    {
        Assert.Equal("C", PlanStatus.Closed.Id);
    }
}
