using Adapters.Strava.Persistence;

namespace Adapters.Strava.Client;

/// <summary>
/// Defines the contract for interacting with the Strava API.
/// </summary>
public interface IStravaApiClient
{
    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens.
    /// </summary>
    /// <param name="code">The authorization code received from Strava's OAuth callback.</param>
    /// <param name="memberId">The Planthor member identifier to associate with the tokens.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the token response, or null if the exchange fails.</returns>
    Task<StravaTokenResponse?> ExchangeCodeAsync(string code, Guid memberId, CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes an expired access token using the stored refresh token.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier whose token should be refreshed.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated token document, or null if the refresh fails.</returns>
    Task<StravaTokenDocument?> RefreshTokenAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a valid access token for the specified member, refreshing it proactively if necessary.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current token document with a valid access token, or null if no token exists or refresh fails.</returns>
    Task<StravaTokenDocument?> GetValidTokenAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>
    /// Deauthorizes the application from the member's Strava account and removes stored tokens.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier to deauthorize.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if deauthorization succeeds or the token is already removed; otherwise, false.</returns>
    Task<bool> DeauthorizeAsync(Guid memberId, CancellationToken cancellationToken);
}
