namespace Adapters.Facebook;

/// <summary>
/// Defines the contract for an adapter that interacts with Facebook's external APIs.
/// Provides methods to retrieve user profile information, such as the profile picture.
/// </summary>
public interface IFacebookAdapter
{
    /// <summary>
    /// Retrieves a stream containing the user's profile picture from the given external path.
    /// </summary>
    /// <param name="externalPath">The external URL path to the profile picture.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A stream of the profile picture, or null if the picture could not be retrieved.</returns>
    Task<Stream?> GetProfilePictureStreamAsync(string externalPath, CancellationToken cancellationToken);
}
