using System.Text.Json.Serialization;

namespace Adapters.Strava.Client;

/// <summary>
/// Represents a single activity as returned by the Strava API
/// (<c>GET /api/v3/athlete/activities</c>).
/// Only the fields relevant to Planthor are mapped.
/// </summary>
public class StravaActivityResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the activity.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the activity.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the distance of the activity in meters.
    /// </summary>
    [JsonPropertyName("distance")]
    public float Distance { get; set; }

    /// <summary>
    /// Gets or sets the time at which the activity was started.
    /// </summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the type of the activity (e.g., Run, Ride).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sport type of the activity.
    /// </summary>
    [JsonPropertyName("sport_type")]
    public string SportType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the athlete that owns the activity.
    /// Used to verify activity ownership against the authenticated user.
    /// </summary>
    [JsonPropertyName("athlete")]
    public StravaActivityAthleteResponse? Athlete { get; set; }
}
