using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Facebook;
using Moq;
using Moq.Protected;

namespace Adapters.Tests.Facebook;

public class FacebookProfileAdapterTests
{
    private static (Mock<HttpMessageHandler> handler, IHttpClientFactory factory) CreateFactory()
    {
        var handler = new Mock<HttpMessageHandler>();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(new HttpClient(handler.Object));
        return (handler, factory.Object);
    }

    private static void SetupUrl(Mock<HttpMessageHandler> handler, string url, HttpStatusCode status, string? content = null)
    {
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = content is null
                    ? new ByteArrayContent([])
                    : new StringContent(content, Encoding.UTF8, "image/jpeg")
            });
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_PrimarySucceeds_ReturnsStream()
    {
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.OK, "image-data");

        var adapter = new FacebookProfileAdapter(factory);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_PrimarySucceeds_FallbackNeverCalled()
    {
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.OK, "image-data");

        var adapter = new FacebookProfileAdapter(factory);
        await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        handler.Protected().Verify("SendAsync",
            Times.Never(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("gravatar")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_PrimaryFails_FallbackSucceeds_ReturnsFallbackStream()
    {
        const string gravatarUrl = "https://www.gravatar.com/avatar/?d=mp&s=200";
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.NotFound);
        SetupUrl(handler, gravatarUrl, HttpStatusCode.OK, "default-avatar-data");

        var adapter = new FacebookProfileAdapter(factory);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_BothFail_ReturnsNull()
    {
        const string gravatarUrl = "https://www.gravatar.com/avatar/?d=mp&s=200";
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.InternalServerError);
        SetupUrl(handler, gravatarUrl, HttpStatusCode.ServiceUnavailable);

        var adapter = new FacebookProfileAdapter(factory);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.Null(stream);
    }
}
