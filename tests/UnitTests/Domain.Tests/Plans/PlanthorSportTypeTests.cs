using System;
using Domain.Plans;
using Xunit;

namespace Domain.Tests.Plans;

public class PlanthorSportTypeTests
{
    [Fact]
    public void FromId_ShouldReturnRun_WhenIdIsRUN()
    {
        var result = PlanthorSportType.FromId("RUN");
        Assert.Equal(PlanthorSportType.Run, result);
    }

    [Fact]
    public void FromId_ShouldReturnAll_WhenIdIsALL()
    {
        var result = PlanthorSportType.FromId("ALL");
        Assert.Equal(PlanthorSportType.All, result);
    }

    [Fact]
    public void FromId_ShouldThrowArgumentException_WhenIdIsInvalid()
    {
        Assert.Throws<ArgumentException>(() => PlanthorSportType.FromId("INVALID"));
    }

    [Fact]
    public void TryFromId_ShouldReturnTrue_WhenIdIsRUN()
    {
        var success = PlanthorSportType.TryFromId("RUN", out var result);
        Assert.True(success);
        Assert.Equal(PlanthorSportType.Run, result);
    }

    [Fact]
    public void TryFromId_ShouldReturnFalse_WhenIdIsInvalid()
    {
        var success = PlanthorSportType.TryFromId("INVALID", out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_EFCore_ShouldCreateEmpty()
    {
        var ctor = typeof(PlanthorSportType).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);
        var instance = (PlanthorSportType)ctor!.Invoke(null);
        Assert.Null(instance.Id);
        Assert.Null(instance.Name);
        Assert.Null(instance.I18NKey);
    }
}
