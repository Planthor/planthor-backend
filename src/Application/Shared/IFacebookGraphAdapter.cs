using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared;

/// <summary>
/// Interface for fetching FB social provider profile avatars.
/// </summary>
public interface IFacebookGraphAdapter
{
    Task<Stream> GetUserProfilePictureStreamAsync(string facebookUserId, CancellationToken cancellationToken);
}
