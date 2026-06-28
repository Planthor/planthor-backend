using Infrastructure.Services;

namespace Infrastructure.Tests.Services;

public class AvatarFileExtensionsTests
{
    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/png", "png")]
    [InlineData("image/gif", "gif")]
    [InlineData("image/webp", "webp")]
    [InlineData("image/bmp", "jpg")]
    [InlineData("application/octet-stream", "jpg")]
    [InlineData("", "jpg")]
    public void GetExtension_ReturnsExpectedExtension(string contentType, string expected)
    {
        var result = AvatarFileExtensions.GetExtension(contentType);
        Assert.Equal(expected, result);
    }
}
