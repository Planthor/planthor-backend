using Api.Routing;

namespace Api.Tests.Routing;

public class SlugifyParameterTransformerTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Members", "members")]
    [InlineData("ExternalConnections", "external-connections")]
    [InlineData("ActivityLogs", "activity-logs")]
    [InlineData("PersonalPlans", "personal-plans")]
    [InlineData("SportTypes", "sport-types")]
    [InlineData("already-kebab", "already-kebab")]
    [InlineData("API", "api")] // test consecutive capitals or just standard behaviour
    public void TransformOutbound_WithVariousInputs_ReturnsExpectedKebabCase(object? input, string? expected)
    {
        // Arrange
        var sut = new SlugifyParameterTransformer();

        // Act
        var result = sut.TransformOutbound(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
