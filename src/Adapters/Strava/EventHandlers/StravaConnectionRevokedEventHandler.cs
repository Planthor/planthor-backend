using System.Globalization;
using Adapters.Strava.Client;
using Adapters.Strava.Persistence;
using Application.Shared;
using Domain.Members;
using Domain.Members.Events;
using Microsoft.Extensions.Logging;

namespace Adapters.Strava.EventHandlers;

/// <summary>
/// Handles the revocation of a Strava connection by permanently removing credentials,
/// canceling pending jobs, and cleaning up queued adapter work.
/// </summary>
/// <param name="stravaClient">The Strava API client used to deauthorize the integration upstream.</param>
/// <param name="tokenDatabase">The database used to retrieve the athlete's token for deauthorization.</param>
/// <param name="activitySyncAdapter">The adapter used to delete operational data associated with the user.</param>
/// <param name="backgroundJobClient">The client used to cancel any pending background sync jobs.</param>
/// <param name="logger">The logger used to record warnings if upstream cleanup fails.</param>
public sealed partial class StravaConnectionRevokedEventHandler(
    IStravaApiClient stravaClient,
    StravaAdapterDatabase tokenDatabase,
    StravaActivitySyncAdapter activitySyncAdapter,
    IBackgroundJobClient backgroundJobClient,
    ILogger<StravaConnectionRevokedEventHandler> logger)
    : IDomainEventHandler<ExternalConnectionRevokedEvent>
{
    private readonly IStravaApiClient _stravaClient = stravaClient ?? throw new ArgumentNullException(nameof(stravaClient));
    private readonly StravaAdapterDatabase _tokenDatabase = tokenDatabase ?? throw new ArgumentNullException(nameof(tokenDatabase));
    private readonly StravaActivitySyncAdapter _activitySyncAdapter = activitySyncAdapter ?? throw new ArgumentNullException(nameof(activitySyncAdapter));
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public Task HandleAsync(
        ExternalConnectionRevokedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return Core();

        async Task Core()
        {
            if (domainEvent.Provider != ExternalProvider.Strava ||
                domainEvent.Type != ExternalConnectionType.ActivitiesSync)
            {
                return;
            }

            try
            {
                await _backgroundJobClient.CancelExternalActivitySyncAsync(
                    ExternalProvider.Strava.Id,
                    domainEvent.ExternalUserId,
                    cancellationToken);

                if (long.TryParse(
                        domainEvent.ExternalUserId,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var athleteId) &&
                    await _tokenDatabase.GetByAthleteIdAsync(athleteId, cancellationToken) is { } token)
                {
                    await _stravaClient.DeauthorizeAsync(token.Id, cancellationToken);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogUpstreamCleanupFailed(exception, domainEvent.ExternalUserId);
            }
            finally
            {
                await _activitySyncAdapter.DeleteOperationalDataAsync(
                    domainEvent.ExternalUserId,
                    CancellationToken.None);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Strava upstream deauthorization failed for athlete {ExternalUserId}; local data is still deleted")]
    private partial void LogUpstreamCleanupFailed(Exception exception, string externalUserId);
}
