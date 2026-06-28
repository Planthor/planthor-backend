using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Adapters.Facebook;

public interface IFacebookAdapter
{
    Task<Stream?> GetProfilePictureStreamAsync(string externalPath, CancellationToken cancellationToken);
}
