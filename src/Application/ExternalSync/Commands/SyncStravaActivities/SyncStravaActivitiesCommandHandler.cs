using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Application.ExternalSync.Commands.SyncStravaActivities;

/// <summary>
/// Handles the execution of <see cref="SyncStravaActivitiesCommand"/>.
/// Fetches recent activities from Strava for a connected member and adds activity logs to their linked plans.
/// </summary>
public class SyncStravaActivitiesCommandHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository,
    IServiceProvider serviceProvider,
    IClock clock)
    : ICommandHandler<SyncStravaActivitiesCommand, int>
{
    /// <summary>
    /// Processes the synchronization of Strava activities for a member's plans.
    /// </summary>
    /// <param name="request">The synchronization command containing the member's identify name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The total number of activity logs successfully created.</returns>
    /// <exception cref="ArgumentException">Thrown when the member is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the member does not have an active Strava activities sync connection.</exception>
    public async Task<int> Handle(SyncStravaActivitiesCommand request, CancellationToken ct)
    {
        var member = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, ct);
        if (member == null)
            throw new ArgumentException("Member not found", nameof(request.IdentifyName));

        if (!member.HasActiveConnection(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync))
            throw new InvalidOperationException("No active Strava connection.");

        var linkedPlans = member.PersonalPlans
            .Where(pp => pp.LinkUserAdapter)
            .ToList();

        if (linkedPlans.Count == 0) return 0;

        var stravaAdapter = serviceProvider.GetRequiredKeyedService<IActivitySyncAdapter>("STRAVA");
        var sportTypeMapper = serviceProvider.GetRequiredKeyedService<IProviderSportTypeMapper>("STRAVA");

        var activities = await stravaAdapter.FetchActivitiesAsync(member.Id, request.IdentifyName, null, ct);

        int logsCreated = 0;
        foreach (var activity in activities)
        {
            var planthorType = sportTypeMapper.MapToPlanthor(activity.ActivityType ?? "");
            
            foreach (var pp in linkedPlans)
            {
                var plan = await planRepository.GetByIdAsync(pp.PlanId, ct);
                if (plan == null) continue;

                if (plan.ActivityLogs.Any(al =>
                    al.ExternalSource?.ExternalActivityId == activity.ExternalActivityId))
                    continue;

                if (activity.OccurredAt < plan.From || activity.OccurredAt > plan.To)
                    continue;
                
                if (plan.SportPlanDetails is null) 
                    continue;
                
                var sportTypes = plan.SportPlanDetails.SportTypes;
                bool isAll = sportTypes.Contains(PlanthorSportType.All.Id, StringComparer.OrdinalIgnoreCase);
                bool hasMappedType = planthorType != null && sportTypes.Contains(planthorType.Id, StringComparer.OrdinalIgnoreCase);
                
                if (!isAll && !hasMappedType)
                    continue;

                var source = new ExternalActivitySource(ExternalProvider.Strava, activity.ExternalActivityId);
                var value = activity.DistanceMeters ?? 0.0;

                if (plan.Unit.Equals("km", StringComparison.OrdinalIgnoreCase))
                {
                    value /= 1000.0;
                }
                else if (plan.Unit.Equals("mi", StringComparison.OrdinalIgnoreCase))
                {
                    value /= 1609.34;
                }

                var activityLocalDate = activity.OccurredAt
                    .InZone(DateTimeZoneProviders.Tzdb.GetZoneOrNull(plan.Timezone) ?? DateTimeZone.Utc)
                    .Date
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

                plan.AddActivityLog((float)value, activityLocalDate, source, clock, member.Id);
                logsCreated++;
                
                await planRepository.UpdateAsync(plan, ct);
            }
        }

        return logsCreated;
    }
}
