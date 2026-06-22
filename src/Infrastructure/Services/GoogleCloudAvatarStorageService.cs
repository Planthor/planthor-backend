using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public partial class GoogleCloudAvatarStorageService : IAvatarStorageService
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
        _bucketName = configuration["Storage:Gcs:BucketName"]
            ?? throw new InvalidOperationException("Storage:Gcs:BucketName is not configured.");
        _logger = logger;
        _storageClient = storageClient;
    }

    public async Task<string> UploadAvatarAsync(
        Guid memberId,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var objectName = $"{ContainerName}/{memberId}/{Guid.NewGuid()}.{AvatarFileExtensions.GetExtension(contentType)}";

        await _storageClient.UploadObjectAsync(
            _bucketName,
            objectName,
            contentType,
            fileStream,
            cancellationToken: cancellationToken);

        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }

    public Task DeleteAvatarAsync(Uri blobUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(blobUri);
        return DeleteAvatarInternalAsync(blobUri, cancellationToken);
    }

    private async Task DeleteAvatarInternalAsync(Uri blobUri, CancellationToken cancellationToken)
    {
        var prefix = $"https://storage.googleapis.com/{_bucketName}/";
        var uriString = blobUri.AbsoluteUri;
        if (!uriString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            LogUriNotInBucket(blobUri, _bucketName);
            return;
        }

        var objectName = uriString[prefix.Length..];
        await _storageClient.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "URI '{Uri}' does not belong to bucket '{Bucket}'. Skipping delete.")]
    private partial void LogUriNotInBucket(Uri uri, string bucket);
}
