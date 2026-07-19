using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Infrastructure.Tests.Services;

public class GoogleCloudAvatarStorageServiceTests
{
    private const string BucketName = "test-bucket";

    private static IConfiguration CreateConfig(string? bucketName)
    {
        var values = new Dictionary<string, string?>
        {
            ["Storage:Gcs:BucketName"] = bucketName
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static (StorageClient client, GoogleCloudAvatarStorageService service) Create(string? bucketName = BucketName)
    {
        var mockClient = Substitute.For<StorageClient>();
        var logger = Substitute.For<ILogger<GoogleCloudAvatarStorageService>>();
        var service = new GoogleCloudAvatarStorageService(CreateConfig(bucketName), logger, mockClient);
        return (mockClient, service);
    }

    private static void SetupUpload(StorageClient mockClient)
    {
        mockClient.UploadObjectAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<UploadObjectOptions>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<IProgress<IUploadProgress>>())
            .Returns(new Google.Apis.Storage.v1.Data.Object());
    }

    [Fact]
    public void Constructor_MissingBucketName_ThrowsInvalidOperationException()
    {
        var logger = Substitute.For<ILogger<GoogleCloudAvatarStorageService>>();
        var mockClient = Substitute.For<StorageClient>();

        Assert.Throws<InvalidOperationException>(() =>
            new GoogleCloudAvatarStorageService(CreateConfig(null), logger, mockClient));
    }

    [Fact]
    public async Task UploadAvatarAsync_ReturnsPublicGcsUrl()
    {
        var (mockClient, service) = Create();
        SetupUpload(mockClient);

        var memberId = Guid.NewGuid();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image"));

        var result = await service.UploadAvatarAsync(memberId, stream, "image/jpeg", CancellationToken.None);

        Assert.StartsWith($"https://storage.googleapis.com/{BucketName}/avatars/{memberId}/", result);
        Assert.EndsWith(".jpg", result);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/gif", ".gif")]
    [InlineData("image/webp", ".webp")]
    [InlineData("image/bmp", ".jpg")]
    public async Task UploadAvatarAsync_ContentType_MapsToCorrectExtension(string contentType, string expectedExt)
    {
        var (mockClient, service) = Create();
        SetupUpload(mockClient);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image"));

        var result = await service.UploadAvatarAsync(Guid.NewGuid(), stream, contentType, CancellationToken.None);

        Assert.EndsWith(expectedExt, result);
    }

    [Fact]
    public async Task DeleteAvatarAsync_ValidUri_CallsStorageClientWithCorrectObjectName()
    {
        var (mockClient, service) = Create();
        mockClient.DeleteObjectAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DeleteObjectOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        const string objectPath = "avatars/some-member-id/file.jpg";
        var uri = $"https://storage.googleapis.com/{BucketName}/{objectPath}";

        await service.DeleteAvatarAsync(new Uri(uri), CancellationToken.None);

        mockClient.Received(1).DeleteObjectAsync(
            BucketName, objectPath, Arg.Any<DeleteObjectOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAvatarAsync_NullUri_ThrowsArgumentNullException()
    {
        var (_, service) = Create();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.DeleteAvatarAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAvatarAsync_UriNotMatchingBucket_SkipsDelete()
    {
        var (mockClient, service) = Create();

        await service.DeleteAvatarAsync(
            new Uri("https://storage.googleapis.com/wrong-bucket/avatars/file.jpg"),
            CancellationToken.None);

        mockClient.DidNotReceive().DeleteObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DeleteObjectOptions>(), Arg.Any<CancellationToken>());
    }
}
