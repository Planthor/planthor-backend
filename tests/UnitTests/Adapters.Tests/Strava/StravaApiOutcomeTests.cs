using System;
using Adapters.Strava.Client;

namespace Adapters.Tests.Strava;

public sealed class StravaApiOutcomeTests
{
    [Fact]
    public void FromId_WithKnownIdIgnoringCase_ReturnsOutcome()
    {
        // Arrange
        const string Id = "success";

        // Act
        var outcome = StravaApiOutcome.FromId(Id);

        // Assert
        Assert.Same(StravaApiOutcome.Success, outcome);
        Assert.Equal("SUCCESS", outcome.Id);
        Assert.Equal("Success", outcome.Name);
    }

    [Fact]
    public void FromId_WithUnknownId_ThrowsArgumentException()
    {
        // Arrange
        const string Id = "unknown";

        // Act
        var exception = Assert.Throws<ArgumentException>(() => StravaApiOutcome.FromId(Id));

        // Assert
        Assert.Contains(Id, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void All_WhenRead_ReturnsEveryKnownOutcome()
    {
        // Arrange

        // Act
        var outcomes = StravaApiOutcome.All;

        // Assert
        Assert.Equal(
            [
                StravaApiOutcome.Success,
                StravaApiOutcome.NotFound,
                StravaApiOutcome.AuthorizationRequired,
                StravaApiOutcome.RateLimited,
                StravaApiOutcome.TransientFailure
            ],
            outcomes);
    }
}
