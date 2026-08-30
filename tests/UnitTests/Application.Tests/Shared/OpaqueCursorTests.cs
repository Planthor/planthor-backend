using System;
using Application.Shared;

namespace Application.Tests.Shared;

public class OpaqueCursorTests
{
    [Fact]
    public void Encode_WithValidString_ReturnsBase64()
    {
        // Arrange
        var plainText = "test_cursor_123";

        // Act
        var result = OpaqueCursor.Encode(plainText);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(plainText, result);
        
        // Manual decode to verify
        var decodedBytes = Convert.FromBase64String(result);
        var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
        Assert.Equal(plainText, decodedString);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Encode_WithNullOrWhiteSpace_ThrowsArgumentException(string? invalidInput)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => OpaqueCursor.Encode(invalidInput!));
    }

    [Fact]
    public void Decode_WithValidBase64_ReturnsPlainText()
    {
        // Arrange
        var plainText = "test_cursor_456";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var result = OpaqueCursor.Decode(encoded);

        // Assert
        Assert.Equal(plainText, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Decode_WithNullOrWhiteSpace_ReturnsNull(string? invalidInput)
    {
        // Act
        var result = OpaqueCursor.Decode(invalidInput);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Decode_WithInvalidBase64_ReturnsNullAndDoesNotThrow()
    {
        // Arrange
        var invalidBase64 = "this_is_not_base64_!";

        // Act
        var result = OpaqueCursor.Decode(invalidBase64);

        // Assert
        Assert.Null(result);
    }
}
