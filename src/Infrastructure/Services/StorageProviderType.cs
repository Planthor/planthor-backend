namespace Infrastructure.Services;

/// <summary>
/// Identifies the avatar storage backend to register for the environment.
/// </summary>
public enum StorageProviderType
{
    /// <summary>
    /// Microsoft Azure storage provider.
    /// </summary>
    Azure,
    /// <summary>
    /// Google Cloud storage provider.
    /// </summary>
    Google
}
