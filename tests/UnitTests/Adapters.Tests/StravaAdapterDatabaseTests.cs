using System;
using Adapters.Strava.Persistence;

namespace Adapters.Tests.Strava;

public class StravaAdapterDatabaseTests
{
    [Fact]
    public void Document_Initialization()
    {
        var doc = new StravaTokenDocument
        {
            Id = Guid.NewGuid(),
            AthleteId = 12345,
            AccessToken = "acc",
            RefreshToken = "ref",
            ExpiresAt = 1234567890,
            LastRefreshedAtUtc = DateTime.UtcNow
        };
        
        Assert.NotEqual(Guid.Empty, doc.Id);
        Assert.Equal(12345, doc.AthleteId);
        Assert.Equal("acc", doc.AccessToken);
        Assert.Equal("ref", doc.RefreshToken);
        Assert.Equal(1234567890, doc.ExpiresAt);
    }
}
