using System;
using System.Linq;
using NodaTime;
using NodaTime.Text;

namespace Domain.Plans;

/// <summary>
/// Encapsulates provider-neutral eligibility and matching rules for automatic activity logs.
/// </summary>
public static class ExternalActivityPlanPolicy
{

    /// <summary>
    /// Determines whether a plan may be included in an automatic-sync run snapshot.
    /// </summary>
    /// <param name="plan">The candidate plan.</param>
    /// <param name="linkUserAdapter">The owning personal plan's adapter-link flag.</param>
    /// <returns><c>true</c> when the plan is an active, linked sport plan with logging enabled.</returns>
    public static bool IsEligible(Plan plan, bool linkUserAdapter)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return linkUserAdapter &&
               plan.Status == PlanStatus.Active &&
               plan.EnableActivityLog &&
               plan.SportPlanDetails is not null;
    }

    /// <summary>
    /// Determines whether an activity matches a plan snapshotted by <see cref="IsEligible"/>.
    /// The plan's current status is intentionally not rechecked so an entire historical run can
    /// contribute even when an earlier activity completes the plan.
    /// </summary>
    /// <param name="plan">A plan already selected into the sync snapshot.</param>
    /// <param name="canonicalSportTypeId">The canonical Planthor sport identifier.</param>
    /// <param name="occurredAt">The source activity occurrence instant.</param>
    /// <param name="runUpperBound">The frozen upper bound for this sync run.</param>
    /// <param name="activityLocalDate">Receives the activity date in the plan timezone.</param>
    /// <returns><c>true</c> when sport and inclusive local-date boundaries match.</returns>
    public static bool TryMatch(
        Plan plan,
        string canonicalSportTypeId,
        Instant occurredAt,
        Instant runUpperBound,
        out string activityLocalDate)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        activityLocalDate = string.Empty;
        if (occurredAt > runUpperBound || string.IsNullOrWhiteSpace(canonicalSportTypeId))
        {
            return false;
        }

        var details = plan.SportPlanDetails;
        if (details is null)
        {
            return false;
        }

        var acceptsSport = details.SportTypes.Contains(
            PlanthorSportType.All.Id,
            StringComparer.OrdinalIgnoreCase) || details.SportTypes.Contains(
            canonicalSportTypeId,
            StringComparer.OrdinalIgnoreCase);

        if (!acceptsSport)
        {
            return false;
        }

        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(plan.Timezone);
        if (zone is null)
        {
            return false;
        }

        activityLocalDate = LocalDatePattern.Iso.Format(occurredAt.InZone(zone).Date);
        return string.Compare(activityLocalDate, plan.StartDateLocal, StringComparison.Ordinal) >= 0 &&
               string.Compare(activityLocalDate, plan.EndDateLocal, StringComparison.Ordinal) <= 0;
    }
}
