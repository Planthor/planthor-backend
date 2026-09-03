using Adapters.Strava.Mapping;

namespace Adapters.Tests.Strava.Mapping;

public class StravaSportTypeMapperTests
{

    [Theory]
    [InlineData("Run", "RUN")]
    [InlineData("TrailRun", "RUN")]
    [InlineData("VirtualRun", "RUN")]
    [InlineData("Walk", "WALK")]
    [InlineData("Hike", "HIKE")]
    [InlineData("Ride", "RIDE")]
    [InlineData("MountainBikeRide", "RIDE")]
    [InlineData("GravelRide", "RIDE")]
    [InlineData("EBikeRide", "RIDE")]
    [InlineData("EMountainBikeRide", "RIDE")]
    [InlineData("VirtualRide", "RIDE")]
    [InlineData("Velomobile", "RIDE")]
    [InlineData("Handcycle", "RIDE")]
    [InlineData("Wheelchair", "RIDE")]
    [InlineData("Swim", "SWIM")]
    [InlineData("run", "RUN")] // Test case-insensitivity
    public void MapToPlanthor_ValidStravaSportType_ReturnsMappedType(string stravaType, string expectedPlanthorId)
    {
        // Act
        var result = StravaSportTypeMapper.MapToCanonicalId(stravaType);

        // Assert
        Assert.Equal(expectedPlanthorId, result);
    }

    [Fact]
    public void MapToPlanthor_NullOrWhiteSpace_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(StravaSportTypeMapper.MapToCanonicalId(null!));
        Assert.Null(StravaSportTypeMapper.MapToCanonicalId(""));
        Assert.Null(StravaSportTypeMapper.MapToCanonicalId("   "));
    }

    [Fact]
    public void MapToPlanthor_UnknownSportType_ReturnsNull()
    {
        // Act
        var result = StravaSportTypeMapper.MapToCanonicalId("IceSkating");

        // Assert
        Assert.Null(result);
    }
}
