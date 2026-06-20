using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class GoogleCloudAvatarStorageService(IConfiguration configuration, ILogger<GoogleCloudAvatarStorageService> logger) : IAvatarStorageService
{
    private const string ContainerName = "avatars";
    private readonly string _bucketName = configuration.GetConnectionString("GcsBucketName")
        ?? throw new InvalidOperationException("ConnectionStrings:GcsBucketName is not configured.");

    // Resolved once at construction using ADC — safe on Cloud Run (credentials fetched lazily on first request)
    private readonly StorageClient _storageClient = StorageClient.Create();

    public async Task<string> UploadAvatarAsync(
        Guid memberId,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var objectName = $"{ContainerName}/{memberId}/{Guid.NewGuid()}.{GetExtension(contentType)}";

        var obj = new Google.Apis.Storage.v1.Data.Object
        {
            Bucket = _bucketName,
            Name = objectName,
            ContentType = contentType
        };

        await _storageClient.UploadObjectAsync(
            obj,
            fileStream,
            new UploadObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead },
            cancellationToken);

        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }

    public async Task DeleteAvatarAsync(string objectUri, CancellationToken cancellationToken)
    {
        var prefix = $"https://storage.googleapis.com/{_bucketName}/";
        if (!objectUri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("URI '{Uri}' does not belong to bucket '{Bucket}'. Skipping delete.", objectUri, _bucketName);
            return;
        }

        var objectName = objectUri[prefix.Length..];
        await _storageClient.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken);
    }

    private static string GetExtension(string contentType) => contentType switch
    {
        "image/jpeg" => "jpg",
        "image/png"  => "png",
        "image/gif"  => "gif",
        "image/webp" => "webp",
        _            => "jpg"
    };
}
