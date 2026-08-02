using System.Text.Json.Serialization;

namespace Adapters.Strava.Client;

/// <summary>
/// Represents the response from Strava's OAuth token exchange endpoint.
/// Contains the access token, refresh token, expiry, and basic athlete info.
/// </summary>
public class StravaTokenResponse
{
    /// <summary>
    /// Gets or sets the access token string.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the refresh token string.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the UTC epoch seconds when the access token expires.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds until the access token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the token type (typically "Bearer").
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the basic athlete information returned with the token.
    /// </summary>
    [JsonPropertyName("athlete")]
    public StravaAthleteInfo Athlete { get; set; } = default!;
}

/// <summary>
/// Minimal athlete information returned in the Strava OAuth token response.
/// </summary>
public class StravaAthleteInfo
{
    /// <summary>
    /// Gets or sets the Strava athlete numeric identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the athlete's first name.
    /// </summary>
    [JsonPropertyName("firstname")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the athlete's last name.
    /// </summary>
    [JsonPropertyName("lastname")]
    public string? LastName { get; set; }
}

/// <summary>
/// Represents the response from Strava's OAuth token refresh endpoint.
/// Does not include athlete information.
/// </summary>
public class StravaRefreshResponse
{
    /// <summary>
    /// Gets or sets the new access token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the new refresh token.
    /// </summary>
    /// <remarks>
    /// Strava may rotate the refresh token. Always persist this value.
    /// </remarks>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the UTC epoch seconds when the new access token expires.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds until the access token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the token type (typically "Bearer").
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = default!;
}
