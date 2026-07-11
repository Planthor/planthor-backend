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
    public void FromId_Expired_ReturnsExpired(string id)
    {
        Assert.Equal(PlanStatus.Expired, PlanStatus.FromId(id));
    }

    [Theory]
    [InlineData("C")]
    [InlineData("c")]
    public void FromId_Completed_ReturnsCompleted(string id)
    {
        Assert.Equal(PlanStatus.Completed, PlanStatus.FromId(id));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("x")]
    public void FromId_Cancelled_ReturnsCancelled(string id)
    {
        Assert.Equal(PlanStatus.Cancelled, PlanStatus.FromId(id));
    }

    [Theory]
    [InlineData("Y")]
    [InlineData("")]
    [InlineData("INVALID")]
    public void FromId_InvalidId_ThrowsArgumentException(string id)
    {
        Assert.Throws<ArgumentException>(() => PlanStatus.FromId(id));
    }

    [Fact]
    public void All_ContainsFiveStatuses()
    {
        Assert.Equal(5, PlanStatus.All.Count);
    }

    [Fact]
    public void All_ContainsAllExpectedStatuses()
    {
        Assert.Contains(PlanStatus.Planned, PlanStatus.All);
        Assert.Contains(PlanStatus.Active, PlanStatus.All);
        Assert.Contains(PlanStatus.Completed, PlanStatus.All);
        Assert.Contains(PlanStatus.Expired, PlanStatus.All);
        Assert.Contains(PlanStatus.Cancelled, PlanStatus.All);
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
    public void Completed_HasCorrectProperties()
    {
        Assert.Equal("C", PlanStatus.Completed.Id);
    }

    [Fact]
    public void Expired_HasCorrectProperties()
    {
        Assert.Equal("E", PlanStatus.Expired.Id);
    }

    [Fact]
    public void Cancelled_HasCorrectProperties()
    {
        Assert.Equal("X", PlanStatus.Cancelled.Id);
    }
}
