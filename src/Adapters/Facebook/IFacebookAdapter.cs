namespace Adapters.Facebook;

public interface IFacebookAdapter
{
    Task<Stream?> GetProfilePictureStreamAsync(string externalPath, CancellationToken cancellationToken);
}
