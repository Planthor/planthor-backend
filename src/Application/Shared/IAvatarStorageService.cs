using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared;

/// <summary>
/// Interface for uploading and managing files in external storage (e.g., Azure Blob Storage).
/// </summary>
public interface IAvatarStorageService
{
    /// <summary>
    /// Uploads an avatar image stream and returns the public URI.
    /// </summary>
    /// <returns>The fully qualified URL to the uploaded avatar.</returns>
    Task<string> UploadAvatarAsync(
        Guid memberId,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    ///
    /// </summary>
    /// <param name="blobUri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteAvatarAsync(string blobUri, CancellationToken cancellationToken);
}
