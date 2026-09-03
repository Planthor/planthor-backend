using Adapters.Strava.Controllers;

namespace Adapters.Tests.Strava.Controllers;

public class OAuthStatePayloadTests
{
    [Fact]
    public void Properties_SetAndGet_ReturnExpectedValues()
    {
        var payload = new OAuthStatePayload
        {
            IdentifyName = "user-1",
            Nonce = "nonce-123",
            TimestampUtc = 1234567890L
        };

        Assert.Equal("user-1", payload.IdentifyName);
        Assert.Equal("nonce-123", payload.Nonce);
        Assert.Equal(1234567890L, payload.TimestampUtc);
    }
}
