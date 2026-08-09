using Application.Shared;

namespace Application.ExternalSync.Commands.SyncStravaActivities;

/// <summary>
/// Command to synchronize Strava activities for a specific member.
/// </summary>
/// <param name="IdentifyName">The unique identifying name of the member.</param>
public record SyncStravaActivitiesCommand(string IdentifyName) : ICommand<int>;
