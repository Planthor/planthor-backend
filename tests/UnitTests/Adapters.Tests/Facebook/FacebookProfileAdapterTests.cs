using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Facebook;
using Microsoft.Extensions.Configuration;
using NSubstitute;

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

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public Dictionary<string, HttpResponseMessage> Responses { get; } = new();
        public List<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            Requests.Add(url);
            if (Responses.TryGetValue(url, out var response))
            {
                return Task.FromResult(response);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private static (TestHttpMessageHandler handler, IHttpClientFactory factory) CreateFactory()
    {
        var handler = new TestHttpMessageHandler();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>())
               .Returns(new HttpClient(handler));
        return (handler, factory);
    }

    private static void SetupUrl(TestHttpMessageHandler handler, string url, HttpStatusCode status, string? content = null)
    {
        handler.Responses[url] = new HttpResponseMessage(status)
        {
            Content = content is null
                ? new ByteArrayContent([])
                : new StringContent(content, Encoding.UTF8, "image/jpeg")
        };
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

        Assert.DoesNotContain(handler.Requests, url => url.Contains("ui-avatars"));
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

    [Fact]
    public async Task GetProfilePictureStreamAsync_PrimaryFails_NoFallbackConfigured_ReturnsNull()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var (handler, factory) = CreateFactory();
        SetupUrl(handler, "https://example.com/photo.jpg", HttpStatusCode.NotFound);

        var adapter = new FacebookProfileAdapter(factory, emptyConfig);
        var stream = await adapter.GetProfilePictureStreamAsync("https://example.com/photo.jpg", CancellationToken.None);

        Assert.Null(stream);
    }
}
