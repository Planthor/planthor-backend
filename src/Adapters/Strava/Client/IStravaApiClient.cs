using Adapters.Strava.Persistence;

namespace Adapters.Strava.Client;

/// <summary>Defines the Strava-specific OAuth and activity HTTP boundary.</summary>
public interface IStravaApiClient
{
    /// <summary>Exchanges an authorization code and persists the resulting athlete tokens.</summary>
    /// <param name="code">The authorization code returned by Strava.</param>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The exchanged token response, or <c>null</c> if the exchange failed.</returns>
    Task<StravaTokenResponse?> ExchangeCodeAsync(
        string code,
        string identifyName,
        CancellationToken cancellationToken);

    /// <summary>Refreshes and immediately persists a rotated Strava token pair.</summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The newly refreshed and persisted token document, or <c>null</c> if the refresh failed.</returns>
    Task<StravaTokenDocument?> RefreshTokenAsync(
        string identifyName,
        CancellationToken cancellationToken);

    /// <summary>Gets a usable access token, refreshing proactively when required.</summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A valid token document, or <c>null</c> if no valid token could be obtained.</returns>
    Task<StravaTokenDocument?> GetValidTokenAsync(
        string identifyName,
        CancellationToken cancellationToken);

    /// <summary>Deauthorizes Strava and permanently removes the local token document.</summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><c>true</c> if deauthorization was successful; otherwise, <c>false</c>.</returns>
    Task<bool> DeauthorizeAsync(string identifyName, CancellationToken cancellationToken);

    /// <summary>Fetches one bounded page of athlete activities using the maximum supported page size.</summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="afterEpoch">The epoch timestamp indicating the start of the historical range (exclusive).</param>
    /// <param name="beforeEpoch">The epoch timestamp indicating the end of the historical range (exclusive).</param>
    /// <param name="page">The 1-based page number to fetch.</param>
    /// <param name="perPage">The maximum number of activities to return per page.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A typed API result containing the list of activities or error details.</returns>
    Task<StravaApiResult<IReadOnlyList<StravaActivityResponse>>> GetAthleteActivitiesPageAsync(
        string identifyName,
        long afterEpoch,
        long beforeEpoch,
        int page,
        int perPage,
        CancellationToken cancellationToken);

    /// <summary>Fetches full details for one activity and verifies athlete ownership.</summary>
    /// <param name="identifyName">The internal unique identifier for the user.</param>
    /// <param name="externalActivityId">The Strava activity identifier to fetch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A typed API result containing the activity details or error information.</returns>
    Task<StravaApiResult<StravaActivityResponse>> GetActivityAsync(
        string identifyName,
        string externalActivityId,
        CancellationToken cancellationToken);
}
