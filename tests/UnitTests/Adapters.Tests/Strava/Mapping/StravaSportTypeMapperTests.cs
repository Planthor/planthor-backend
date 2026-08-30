using Adapters.Strava.Mapping;

namespace Adapters.Tests.Strava.Mapping;

public class StravaSportTypeMapperTests
{
    private readonly StravaSportTypeMapper _mapper;

    public StravaSportTypeMapperTests()
    {
        _mapper = new StravaSportTypeMapper();
    }

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
        var result = _mapper.MapToPlanthor(stravaType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPlanthorId, result.Id);
    }

    [Fact]
    public void MapToPlanthor_NullOrWhiteSpace_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(_mapper.MapToPlanthor(null!));
        Assert.Null(_mapper.MapToPlanthor(""));
        Assert.Null(_mapper.MapToPlanthor("   "));
    }

    [Fact]
    public void MapToPlanthor_UnknownSportType_ReturnsNull()
    {
        // Act
        var result = _mapper.MapToPlanthor("IceSkating");

        // Assert
        Assert.Null(result);
    }
}
