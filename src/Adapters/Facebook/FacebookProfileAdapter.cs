using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Abstraction;

namespace Adapters.Facebook;

public class FacebookProfileAdapter(IHttpClientFactory httpClientFactory) : ISocialProfileAdapter
{
    private const string DefaultAvatarUrl = "https://www.gravatar.com/avatar/?d=mp&s=200";

    public string ProviderId => "Facebook";

    public async Task<Stream?> GetProfilePictureStreamAsync(string externalPath, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();

        var response = await client.GetAsync(externalPath, cancellationToken);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStreamAsync(cancellationToken);

        response.Dispose();

        var fallback = await client.GetAsync(DefaultAvatarUrl, cancellationToken);
        if (fallback.IsSuccessStatusCode)
            return await fallback.Content.ReadAsStreamAsync(cancellationToken);

        fallback.Dispose();
        return null;
    }
}
