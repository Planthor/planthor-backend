using Application.Dtos;
using NodaTime;

namespace Adapters.Tests.Abstraction;

public class AdapterActivityDtoTests
{
    [Fact]
    public void Properties_Work()
    {
        var instant = SystemClock.Instance.GetCurrentInstant();
        var duration = Duration.FromHours(1);
        var dto = new AdapterActivityDto(
            "123",
            "Provider",
            "Name",
            instant,
            "Run",
            10.0,
            duration
        );
        
        Assert.Equal("123", dto.ExternalActivityId);
        Assert.Equal("Provider", dto.ProviderId);
        Assert.Equal("Name", dto.Name);
        Assert.Equal(instant, dto.OccurredAt);
        Assert.Equal("Run", dto.ActivityType);
        Assert.Equal(10.0, dto.DistanceMeters);
        Assert.Equal(duration, dto.MovingTime);
    }
}
