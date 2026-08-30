namespace Adapters.Strava.Configuration;

/// <summary>
/// Strongly-typed configuration options for the Strava integration adapter.
/// Bound from the <c>Strava</c> configuration section.
/// </summary>
/// <remarks>
/// Sensitive values (<see cref="ClientSecret"/>, <see cref="WebhookVerifyToken"/>,
/// <see cref="StateEncryptionKey"/>) should be stored in Azure Key Vault or
/// ASP.NET User Secrets — never committed to source control.
/// </remarks>
public sealed class StravaOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public static readonly string SectionName = "Strava";

    /// <summary>
    /// Gets or sets the Strava API application client ID.
    /// </summary>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Strava API application client secret.
    /// </summary>
    public string ClientSecret { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URL Strava redirects to after the user authorizes.
    /// </summary>
    /// <example>https://your-domain.com/strava/callback</example>
    public Uri RedirectUri { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URL the frontend should be redirected to after a successful OAuth callback.
    /// </summary>
    /// <example>https://app.planthor.space/settings/connections</example>
    public Uri FrontendSuccessUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URL the frontend should be redirected to after a failed OAuth callback.
    /// </summary>
    /// <example>https://app.planthor.space/settings/connections?error=strava</example>
    public Uri FrontendErrorUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets the secret token used to verify Strava webhook subscription handshakes.
    /// </summary>
    public string WebhookVerifyToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the AES-256 key (Base64-encoded, 32 bytes) used to encrypt
    /// the OAuth <c>state</c> parameter for CSRF protection.
    /// </summary>
    public string StateEncryptionKey { get; set; } = default!;

    /// <summary>
    /// Gets or sets the OAuth scopes to request from Strava.
    /// Defaults to <c>"activity:read_all,profile:read_all"</c>.
    /// </summary>
    public string Scopes { get; set; } = "activity:read_all,profile:read_all";

    /// <summary>
    /// Gets or sets the Strava API Base URL (useful for mocking).
    /// </summary>
    public Uri BaseUrl { get; set; } = new Uri("https://www.strava.com");
}
