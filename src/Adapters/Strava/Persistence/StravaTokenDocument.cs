namespace Adapters.Strava.Persistence;

/// <summary>
/// Persists a member's Strava OAuth tokens and incremental sync watermark.
/// Stored in the <c>strava_adapter_db / strava_tokens</c> collection.
/// </summary>
/// <remarks>
/// This document uses the Planthor <c>IdentifyName</c> (Keycloak Subject ID) as its primary key,
/// ensuring a one-to-one mapping between a member's identity provider ID and their Strava credentials.
/// The <see cref="AthleteId"/> field enables reverse lookups when processing
/// webhook events (which only carry the Strava athlete ID, not the Planthor member ID).
/// </remarks>
public sealed class StravaTokenDocument
{
    /// <summary>
    /// Gets or sets the document identifier, which equals the Planthor member's IdentifyName.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Strava athlete numeric identifier.
    /// Used for reverse lookups from webhook payloads.
    /// </summary>
    public long AthleteId { get; set; }

    /// <summary>
    /// Gets or sets the current OAuth access token issued by Strava.
    /// </summary>
    public string AccessToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the refresh token used to obtain new access tokens.
    /// </summary>
    /// <remarks>
    /// Strava may rotate the refresh token on every refresh response.
    /// The new value <b>must</b> be persisted immediately to avoid
    /// permanent lockout.
    /// </remarks>
    public string RefreshToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the UTC epoch seconds timestamp at which
    /// <see cref="AccessToken"/> expires.
    /// </summary>
    public long ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the watermark for incremental activity synchronization.
    /// Represents the UTC epoch seconds of the most recently synced activity's start time.
    /// <c>null</c> if no sync has been performed yet.
    /// </summary>
    public long? LastSyncEpoch { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the last successful token refresh or initial token exchange.
    /// </summary>
    public DateTimeOffset LastRefreshedAtUtc { get; set; }
}
