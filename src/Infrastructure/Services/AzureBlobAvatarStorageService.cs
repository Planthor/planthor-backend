using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

/// <summary>
/// Implements <see cref="IAvatarStorageService"/> using Azure Blob Storage.
/// Provides functionality for uploading and deleting member avatar images.
/// </summary>
/// <param name="configuration">The application configuration used to retrieve storage connection strings.</param>
public class AzureBlobAvatarStorageService(IConfiguration configuration) : IAvatarStorageService
{
    private const string ContainerName = "avatars";
    private readonly string _connectionString = configuration.GetConnectionString("AzureStorage")
        ?? throw new InvalidOperationException("AzureStorage connection string is not configured.");

    /// <inheritdoc/>
    public async Task DeleteAvatarAsync(string blobUri, CancellationToken cancellationToken)
    {
        var blobClient = new BlobClient(new Uri(blobUri));
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> UploadAvatarAsync(
        Guid memberId,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobName = $"{memberId}/{Guid.NewGuid()}.{GetExtension(contentType)}";
        var blobClient = containerClient.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken);

        return blobClient.Uri.ToString();
    }

    private static string GetExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            _ => "jpg"
        };
    }
}


