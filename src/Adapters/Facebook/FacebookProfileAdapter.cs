using Microsoft.Extensions.Configuration;

namespace Adapters.Facebook;

public class FacebookProfileAdapter(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IFacebookAdapter
{
    private readonly Uri? _fallbackAvatarUri =
        configuration["SocialProfile:Facebook:FallbackAvatarUrl"] is { } url ? new Uri(url) : null;

    public async Task<Stream?> GetProfilePictureStreamAsync(string externalPath, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();

        var response = await client.GetAsync(new Uri(externalPath), cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }

        response.Dispose();

        if (_fallbackAvatarUri is null)
        {
            return null;
        }

        var fallback = await client.GetAsync(_fallbackAvatarUri, cancellationToken);
        if (fallback.IsSuccessStatusCode)
        {
            return await fallback.Content.ReadAsStreamAsync(cancellationToken);
        }

        fallback.Dispose();
        return null;
    }
}
