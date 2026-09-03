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

    /// <summary>Gets or sets the initial historical synchronization state.</summary>
    public string InitialSyncState { get; set; } = "not_started";

    /// <summary>Gets or sets the most recent synchronization state.</summary>
    public string SyncState { get; set; } = "idle";

    /// <summary>Gets or sets the most recent provider-neutral trigger kind.</summary>
    public string? LastSyncTrigger { get; set; }

    /// <summary>Gets or sets when the most recent synchronization started.</summary>
    public DateTimeOffset? LastSyncStartedAtUtc { get; set; }

    /// <summary>Gets or sets when synchronization most recently completed successfully.</summary>
    public DateTimeOffset? LastSuccessfulSyncAtUtc { get; set; }

    /// <summary>Gets or sets the earliest time deferred work should resume.</summary>
    public DateTimeOffset? NextSyncAttemptAtUtc { get; set; }

    /// <summary>Gets or sets a stable machine-readable synchronization error code.</summary>
    public string? SyncErrorCode { get; set; }

    /// <summary>Gets or sets the total number of ActivityLogs created by successful runs.</summary>
    public long ActivityLogsCreated { get; set; }
}
