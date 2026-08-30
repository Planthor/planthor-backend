using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
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
public sealed class SyncStravaActivitiesCommandHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository,
    IServiceProvider serviceProvider,
    IClock clock) : ICommandHandler<SyncStravaActivitiesCommand, int>
{
    private const double MetersInKilometer = 1000.0;
    private const double MetersInMile = 1609.34;
    private const string DateFormat = "yyyy-MM-dd";
    private const string ProviderName = "STRAVA";

    private readonly IMemberRepository _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
    private readonly IPlanRepository _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>
    /// Processes the synchronization of Strava activities for a member's plans.
    /// </summary>
    /// <param name="request">The synchronization command containing the member's identify name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of activity logs successfully created.</returns>
    /// <exception cref="ArgumentException">Thrown when the member is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the member does not have an active Strava activities sync connection.</exception>
    public async Task<int> Handle(SyncStravaActivitiesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await _memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken);
        if (member == null)
        {
            throw new ArgumentException("Member not found", nameof(request));
        }

        if (!member.HasActiveConnection(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync))
        {
            throw new InvalidOperationException("No active Strava connection.");
        }

        var linkedPlans = member.PersonalPlans
            .Where(pp => pp.LinkUserAdapter)
            .ToList();

        if (linkedPlans.Count == 0)
        {
            return 0;
        }

        var stravaAdapter = _serviceProvider.GetRequiredKeyedService<IActivitySyncAdapter>(ProviderName);
        var sportTypeMapper = _serviceProvider.GetRequiredKeyedService<IProviderSportTypeMapper>(ProviderName);

        var activities = await stravaAdapter.FetchActivitiesAsync(member.Id, request.IdentifyName, null, cancellationToken);

        int logsCreated = 0;
        foreach (var activity in activities)
        {
            logsCreated += await ProcessActivityAsync(activity, linkedPlans, member.Id, sportTypeMapper, cancellationToken);
        }

        return logsCreated;
    }

    private async Task<int> ProcessActivityAsync(
        AdapterActivityDto activity,
        List<PersonalPlan> linkedPlans,
        Guid memberId,
        IProviderSportTypeMapper sportTypeMapper,
        CancellationToken cancellationToken)
    {
        int created = 0;
        var planthorType = sportTypeMapper.MapToPlanthor(activity.ActivityType ?? string.Empty);

        foreach (var pp in linkedPlans)
        {
            var plan = await _planRepository.GetByIdAsync(pp.PlanId, cancellationToken);
            if (plan == null)
            {
                continue;
            }

            if (ShouldSkipPlanForActivity(plan, activity, planthorType))
            {
                continue;
            }

            var source = new ExternalActivitySource(ExternalProvider.Strava, activity.ExternalActivityId);
            var value = activity.DistanceMeters ?? 0.0;

            if (plan.Unit.Equals("km", StringComparison.OrdinalIgnoreCase))
            {
                value /= MetersInKilometer;
            }
            else if (plan.Unit.Equals("mi", StringComparison.OrdinalIgnoreCase))
            {
                value /= MetersInMile;
            }

            var activityLocalDate = activity.OccurredAt
                .InZone(DateTimeZoneProviders.Tzdb.GetZoneOrNull(plan.Timezone) ?? DateTimeZone.Utc)
                .Date
                .ToString(DateFormat, System.Globalization.CultureInfo.InvariantCulture);

            plan.AddActivityLog((float)value, activityLocalDate, source, _clock, memberId);
            created++;
            
            await _planRepository.UpdateAsync(plan, cancellationToken);
        }

        return created;
    }

    private static bool ShouldSkipPlanForActivity(Plan plan, AdapterActivityDto activity, PlanthorSportType? planthorType)
    {
        if (plan.ActivityLogs.Any(al => al.ExternalSource?.ExternalActivityId == activity.ExternalActivityId))
        {
            return true;
        }

        if (activity.OccurredAt < plan.From || activity.OccurredAt > plan.To)
        {
            return true;
        }
        
        if (plan.SportPlanDetails is null) 
        {
            return true;
        }
        
        var sportTypes = plan.SportPlanDetails.SportTypes;
        bool isAll = sportTypes.Contains(PlanthorSportType.All.Id, StringComparer.OrdinalIgnoreCase);
        bool hasMappedType = planthorType != null && sportTypes.Contains(planthorType.Id, StringComparer.OrdinalIgnoreCase);
        
        if (!isAll && !hasMappedType)
        {
            return true;
        }

        return false;
    }
}
