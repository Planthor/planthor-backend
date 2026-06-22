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
using Moq;

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

    private static (Mock<StorageClient> client, GoogleCloudAvatarStorageService service) Create(string? bucketName = BucketName)
    {
        var mockClient = new Mock<StorageClient>();
        var logger = new Mock<ILogger<GoogleCloudAvatarStorageService>>();
        var service = new GoogleCloudAvatarStorageService(CreateConfig(bucketName), logger.Object, mockClient.Object);
        return (mockClient, service);
    }

    private static void SetupUpload(Mock<StorageClient> mockClient)
    {
        mockClient.Setup(s => s.UploadObjectAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<UploadObjectOptions>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<IUploadProgress>>()))
            .ReturnsAsync(new Google.Apis.Storage.v1.Data.Object());
    }

    [Fact]
    public void Constructor_MissingBucketName_ThrowsInvalidOperationException()
    {
        var logger = new Mock<ILogger<GoogleCloudAvatarStorageService>>();
        var mockClient = new Mock<StorageClient>();

        Assert.Throws<InvalidOperationException>(() =>
            new GoogleCloudAvatarStorageService(CreateConfig(null), logger.Object, mockClient.Object));
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
        mockClient.Setup(s => s.DeleteObjectAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DeleteObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        const string objectPath = "avatars/some-member-id/file.jpg";
        var uri = $"https://storage.googleapis.com/{BucketName}/{objectPath}";

        await service.DeleteAvatarAsync(new Uri(uri), CancellationToken.None);

        mockClient.Verify(s => s.DeleteObjectAsync(
            BucketName, objectPath, It.IsAny<DeleteObjectOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAvatarAsync_UriNotMatchingBucket_SkipsDelete()
    {
        var (mockClient, service) = Create();

        await service.DeleteAvatarAsync(
            new Uri("https://storage.googleapis.com/wrong-bucket/avatars/file.jpg"),
            CancellationToken.None);

        mockClient.Verify(s => s.DeleteObjectAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DeleteObjectOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
