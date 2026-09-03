using System.Text.Json.Serialization;

namespace Adapters.Strava.Controllers;

/// <summary>
/// Internal payload serialized into the encrypted OAuth <c>state</c> parameter.
/// </summary>
internal sealed class OAuthStatePayload
{
    /// <summary>
    /// Gets or sets the Planthor member's Keycloak subject ID (Identity Name).
    /// </summary>
    [JsonInclude]
    internal string IdentifyName { get; set; } = default!;

    /// <summary>
    /// Gets or sets a cryptographic nonce to prevent replay attacks.
    /// </summary>
    [JsonInclude]
    internal string Nonce { get; set; } = default!;

    /// <summary>
    /// Gets or sets the UTC epoch seconds when the state was created.
    /// </summary>
    [JsonInclude]
    internal long TimestampUtc { get; set; }
}
