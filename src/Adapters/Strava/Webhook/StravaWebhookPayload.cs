using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adapters.Strava.Webhook;

/// <summary>Represents the complete event envelope delivered by a Strava webhook subscription.</summary>
public sealed class StravaWebhookPayload
{
    /// <summary>Gets or sets the resource kind, such as <c>activity</c> or <c>athlete</c>.</summary>
    [JsonPropertyName("object_type")]
    public string ObjectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the activity or athlete resource identifier.</summary>
    [JsonPropertyName("object_id")]
    public long ObjectId { get; set; }

    /// <summary>Gets or sets the change aspect: create, update, or delete.</summary>
    [JsonPropertyName("aspect_type")]
    public string AspectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the athlete that owns the event resource.</summary>
    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }

    /// <summary>Gets or sets the application-level webhook subscription identifier.</summary>
    [JsonPropertyName("subscription_id")]
    public long SubscriptionId { get; set; }

    /// <summary>Gets or sets the event occurrence time in Unix epoch seconds.</summary>
    [JsonPropertyName("event_time")]
    public long EventTime { get; set; }

    /// <summary>Gets or sets changed athlete properties supplied with update events.</summary>
    [JsonPropertyName("updates")]
    public Dictionary<string, JsonElement> Updates { get; set; } = [];

    /// <summary>Returns whether this event reports that the athlete revoked authorization.</summary>
    public bool IsDeauthorization()
    {
        if (!ObjectType.Equals("athlete", StringComparison.OrdinalIgnoreCase) ||
            !AspectType.Equals("update", StringComparison.OrdinalIgnoreCase) ||
            !Updates.TryGetValue("authorized", out var authorized))
        {
            return false;
        }

        return authorized.ValueKind switch
        {
            JsonValueKind.False => true,
            JsonValueKind.String => bool.TryParse(authorized.GetString(), out var value) && !value,
            _ => false
        };
    }

    /// <summary>Builds the deterministic delivery key used to coalesce duplicate webhook events.</summary>
    public string IdempotencyKey =>
        $"{SubscriptionId}:{OwnerId}:{ObjectType}:{ObjectId}:{AspectType}:{EventTime}";
}
