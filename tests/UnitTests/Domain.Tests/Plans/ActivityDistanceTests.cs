using Domain.Plans;
using Xunit;

namespace Domain.Tests.Plans;

public sealed class ActivityDistanceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConvertMeters_WithInvalidDistance_ReturnsNull(double? meters)
    {
        // Arrange
        const string Unit = "km";

        // Act
        var result = ActivityDistance.ConvertMeters(meters, Unit);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(1000d, "M", 1000f)]
    [InlineData(1000d, " km ", 1f)]
    [InlineData(1609.344d, "mi", 1f)]
    [InlineData(0.9144d, "YD", 1f)]
    public void ConvertMeters_WithSupportedUnit_ReturnsConvertedDistance(
        double meters,
        string unit,
        float expected)
    {
        // Arrange

        // Act
        var result = ActivityDistance.ConvertMeters(meters, unit);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("parsecs")]
    public void ConvertMeters_WithUnsupportedUnit_ReturnsNull(string unit)
    {
        // Arrange
        const double Meters = 1000d;

        // Act
        var result = ActivityDistance.ConvertMeters(Meters, unit);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertMeters_WhenConvertedValueExceedsFloatRange_ReturnsNull()
    {
        // Arrange
        const double Meters = double.MaxValue;

        // Act
        var result = ActivityDistance.ConvertMeters(Meters, "m");

        // Assert
        Assert.Null(result);
    }
}
