using System;
using Application.Shared;

namespace Application.Tests.Shared;

public sealed class ExternalActivitySyncTriggerTests
{
    [Fact]
    public void FromName_WithKnownNameIgnoringCase_ReturnsTrigger()
    {
        // Arrange
        const string Name = "WEBHOOK";

        // Act
        var trigger = ExternalActivitySyncTrigger.FromName(Name);

        // Assert
        Assert.Same(ExternalActivitySyncTrigger.Webhook, trigger);
        Assert.Equal("webhook", trigger.Name);
        Assert.Equal("webhook", trigger.ToString());
    }

    [Fact]
    public void FromName_WithUnknownName_ThrowsArgumentException()
    {
        // Arrange
        const string Name = "unknown";

        // Act
        var exception = Assert.Throws<ArgumentException>(() =>
            ExternalActivitySyncTrigger.FromName(Name));

        // Assert
        Assert.Contains(Name, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void All_WhenRead_ReturnsEveryKnownTrigger()
    {
        // Arrange

        // Act
        var triggers = ExternalActivitySyncTrigger.All;

        // Assert
        Assert.Equal(
            [
                ExternalActivitySyncTrigger.Initial,
                ExternalActivitySyncTrigger.Manual,
                ExternalActivitySyncTrigger.Webhook,
                ExternalActivitySyncTrigger.Retry
            ],
            triggers);
    }

    [Fact]
    public void ImplicitConversions_WithValidValues_RoundTrip()
    {
        // Arrange
        ExternalActivitySyncTrigger trigger = "manual";

        // Act
        string name = trigger;

        // Assert
        Assert.Same(ExternalActivitySyncTrigger.Manual, trigger);
        Assert.Equal("manual", name);
    }

    [Fact]
    public void ImplicitStringConversion_WithNullTrigger_ThrowsArgumentNullException()
    {
        // Arrange
        ExternalActivitySyncTrigger? trigger = null;

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            string _ = trigger!;
        });

        // Assert
        Assert.Equal("trigger", exception.ParamName);
    }

    [Fact]
    public void Equality_WithSameDifferentAndNullValues_ReturnsExpectedResults()
    {
        // Arrange
        var initial = ExternalActivitySyncTrigger.Initial;
        var initialFromName = ExternalActivitySyncTrigger.FromName("INITIAL");
        var manual = ExternalActivitySyncTrigger.Manual;
        var sameReferenceLeft = ExternalActivitySyncTrigger.Initial;
        var sameReferenceRight = ExternalActivitySyncTrigger.Initial;

        // Act
        var sameReference = sameReferenceLeft == sameReferenceRight;
        var sameValue = initial.Equals(initialFromName);
        var different = initial != manual;
        var equalsNull = initial.Equals(null);
        var equalsObject = initial!.Equals((object)initialFromName!);
        var leftNull = (ExternalActivitySyncTrigger?)null == manual;
        var rightNull = initial == (ExternalActivitySyncTrigger?)null;
        var bothNull = (ExternalActivitySyncTrigger?)null == null;

        // Assert
        Assert.True(sameReference);
        Assert.True(sameValue);
        Assert.True(different);
        Assert.False(leftNull);
        Assert.False(rightNull);
        Assert.True(bothNull);
        Assert.False(equalsNull);
        Assert.True(equalsObject);
        Assert.Equal(initial!.GetHashCode(), initialFromName!.GetHashCode());
    }
}
