using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class GoogleCloudAvatarStorageService : IAvatarStorageService
{
    private const string ContainerName = "avatars";
    private readonly string _bucketName;
    private readonly StorageClient _storageClient;
    private readonly ILogger<GoogleCloudAvatarStorageService> _logger;

    public GoogleCloudAvatarStorageService(IConfiguration configuration, ILogger<GoogleCloudAvatarStorageService> logger)
        : this(configuration, logger, StorageClient.Create()) { }

    // Resolved once at construction using ADC — safe on Cloud Run (credentials fetched lazily on first request)
    internal GoogleCloudAvatarStorageService(IConfiguration configuration, ILogger<GoogleCloudAvatarStorageService> logger, StorageClient storageClient)
    {
        _bucketName = configuration.GetConnectionString("GcsBucketName")
            ?? throw new InvalidOperationException("ConnectionStrings:GcsBucketName is not configured.");
        _logger = logger;
        _storageClient = storageClient;
    }

    public async Task<string> UploadAvatarAsync(
        Guid memberId,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var objectName = $"{ContainerName}/{memberId}/{Guid.NewGuid()}.{GetExtension(contentType)}";

        await _storageClient.UploadObjectAsync(
            _bucketName,
            objectName,
            contentType,
            fileStream,
            new UploadObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead },
            cancellationToken);

        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }

    public async Task DeleteAvatarAsync(Uri blobUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(blobUri);

        var prefix = $"https://storage.googleapis.com/{_bucketName}/";
        var uriString = blobUri.AbsoluteUri;
        if (!uriString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("URI '{Uri}' does not belong to bucket '{Bucket}'. Skipping delete.", blobUri, _bucketName);
            return;
        }

        var objectName = uriString[prefix.Length..];
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
