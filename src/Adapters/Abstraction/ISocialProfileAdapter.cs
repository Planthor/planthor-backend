namespace Adapters.Abstraction;

/// <summary>
/// Defines the contract for an adapter that interacts with external APIs to retrieve social profile data.
/// Implementations are registered with keyed DI using ProviderId as the key.
/// </summary>
public interface ISocialProfileAdapter
{
    /// <summary>
    /// The external provider this adapter serves.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Retrieves a stream containing the user's profile picture from the given external path.
    /// </summary>
    /// <param name="externalPath">The external URL path to the profile picture.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A stream of the profile picture, or null if the picture could not be retrieved.</returns>
    Task<Stream?> GetProfilePictureStreamAsync(string externalPath, CancellationToken cancellationToken);
}
