using Api.Routing;

namespace Api.Tests.Routing;

public class SlugifyParameterTransformerTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Members", "MEMBERS")]
    [InlineData("ExternalConnections", "EXTERNAL-CONNECTIONS")]
    [InlineData("ActivityLogs", "ACTIVITY-LOGS")]
    [InlineData("PersonalPlans", "PERSONAL-PLANS")]
    [InlineData("SportTypes", "SPORT-TYPES")]
    [InlineData("already-kebab", "ALREADY-KEBAB")]
    [InlineData("API", "API")] // test consecutive capitals or just standard behaviour
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
