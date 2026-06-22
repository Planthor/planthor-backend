using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Abstraction;
using Microsoft.Extensions.Configuration;

namespace Adapters.Facebook;

public class FacebookProfileAdapter(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ISocialProfileAdapter
{
    private const string DefaultFallbackAvatarUrl = "https://ui-avatars.com/api/?name=Planthor+User&background=random&size=200";

    private readonly Uri _fallbackAvatarUri = new(
        configuration["SocialProfile:Facebook:FallbackAvatarUrl"] ?? DefaultFallbackAvatarUrl);

    public string ProviderId => "Facebook";

    public async Task<Stream?> GetProfilePictureStreamAsync(string externalPath, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();

        var response = await client.GetAsync(new Uri(externalPath), cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }

        response.Dispose();

        var fallback = await client.GetAsync(_fallbackAvatarUri, cancellationToken);
        if (fallback.IsSuccessStatusCode)
        {
            return await fallback.Content.ReadAsStreamAsync(cancellationToken);
        }

        fallback.Dispose();
        return null;
    }
}
