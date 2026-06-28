namespace Infrastructure.Services;

/// <summary>
/// Maps avatar image content types to file extensions.
/// Shared by the storage provider implementations.
/// </summary>
internal static class AvatarFileExtensions
{
    /// <summary>
    /// Returns the file extension (without dot) for the given image content type.
    /// Defaults to <c>jpg</c> for unknown types.
    /// </summary>
    public static string GetExtension(string contentType) => contentType switch
    {
        "image/jpeg" => "jpg",
        "image/png" => "png",
        "image/gif" => "gif",
        "image/webp" => "webp",
        _ => "jpg"
    };
}
