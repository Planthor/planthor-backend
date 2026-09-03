using Application.Dtos;
using NodaTime;

namespace Adapters.Tests.Abstraction;

public class AdapterActivityDtoTests
{
    [Fact]
    public void Properties_Work()
    {
        var instant = SystemClock.Instance.GetCurrentInstant();
        var dto = new AdapterActivityDto(
            "123",
            "Provider",
            "RUN",
            instant,
            10.0
        );
        
        Assert.Equal("123", dto.ExternalActivityId);
        Assert.Equal("Provider", dto.ProviderId);
        Assert.Equal("RUN", dto.CanonicalSportTypeId);
        Assert.Equal(instant, dto.OccurredAt);
        Assert.Equal(10.0, dto.DistanceMeters);
    }
}
