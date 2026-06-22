using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Facebook;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

namespace Adapters.Tests.Facebook;

public class FacebookProfileAdapterTests
{
    private const string FallbackAvatarUrl = "https://ui-avatars.com/api/?name=Planthor+User&background=random&size=200";

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SocialProfile:Facebook:FallbackAvatarUrl"] = FallbackAvatarUrl
            })
            .Build();

    private static FacebookProfileAdapter CreateAdapter(IHttpClientFactory factory) =>
        new(factory, CreateConfig());

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
    public async Task GetProfilePictureStreamAsync_NullConfigUrl_UsesDefaultFallback()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.NotFound);

        // Default fallback is ui-avatars — set it up to succeed
        const string defaultFallback = "https://ui-avatars.com/api/?name=Planthor+User&background=random&size=200";
        SetupUrl(handler, defaultFallback, HttpStatusCode.OK, "fallback-data");

        var adapter = new FacebookProfileAdapter(factory, emptyConfig);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.NotNull(stream);
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_PrimarySucceeds_ReturnsStream()
    {
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.OK, "image-data");

        var adapter = CreateAdapter(factory);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_PrimarySucceeds_FallbackNeverCalled()
    {
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.OK, "image-data");

        var adapter = CreateAdapter(factory);
        await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        handler.Protected().Verify("SendAsync",
            Times.Never(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("ui-avatars")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_PrimaryFails_FallbackSucceeds_ReturnsFallbackStream()
    {
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.NotFound);
        SetupUrl(handler, FallbackAvatarUrl, HttpStatusCode.OK, "default-avatar-data");

        var adapter = CreateAdapter(factory);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task GetProfilePictureStreamAsync_BothFail_ReturnsNull()
    {
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.InternalServerError);
        SetupUrl(handler, FallbackAvatarUrl, HttpStatusCode.ServiceUnavailable);

        var adapter = CreateAdapter(factory);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.Null(stream);
    }
}
