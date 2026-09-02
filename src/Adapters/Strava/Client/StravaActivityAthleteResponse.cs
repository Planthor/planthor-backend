using System.Text.Json.Serialization;

namespace Adapters.Strava.Client;

/// <summary>
/// Represents the athlete-owner information embedded within a Strava activity response.
/// </summary>
public sealed class StravaActivityAthleteResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the Strava athlete who owns the activity.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
