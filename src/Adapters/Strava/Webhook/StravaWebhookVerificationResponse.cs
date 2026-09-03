using System.Text.Json.Serialization;

namespace Adapters.Strava.Webhook;

/// <summary>Echoes Strava's subscription-verification challenge using the exact required JSON name.</summary>
/// <param name="Challenge">The challenge supplied by Strava.</param>
public sealed record StravaWebhookVerificationResponse(
    [property: JsonPropertyName("hub.challenge")] string Challenge);
