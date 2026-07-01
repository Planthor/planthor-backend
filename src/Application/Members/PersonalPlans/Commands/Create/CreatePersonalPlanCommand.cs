using System;
using Application.Shared;

namespace Application.Members.PersonalPlans.Commands.Create;

/// <summary>
/// Command to create a new plan and subscribe a member to it as a personal plan.
/// </summary>
/// <param name="IdentifyName">The unique identifier or username of the member creating the plan.</param>
/// <param name="Name">The display name of the plan.</param>
/// <param name="Unit">The unit of measurement for this plan's target (e.g., km, hours, steps).</param>
/// <param name="Target">The numeric target this plan aims to reach.</param>
/// <param name="FromDate">The UTC date and time at which this plan period starts.</param>
/// <param name="ToDate">The UTC date and time at which this plan period ends.</param>
/// <param name="StartDateLocal">The local start date as an ISO string (e.g. YYYY-MM-DD).</param>
/// <param name="EndDateLocal">The local end date as an ISO string (e.g. YYYY-MM-DD).</param>
/// <param name="Timezone">The IANA timezone identifier of the member creating the plan.</param>
/// <param name="EnableActivityLog">Whether activity logging is enabled for this plan.</param>
/// <param name="DisplayOnProfile">Whether this plan is displayed on the member's public profile.</param>
/// <param name="Prioritize">The display priority of this plan on the member's profile (0-999).</param>
/// <param name="LinkUserAdapter">Whether Strava or other external activity sync is enabled for this plan.</param>
public record CreatePersonalPlanCommand(
    string IdentifyName,
    string Name,
    string Unit,
    double Target,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string StartDateLocal,
    string EndDateLocal,
    string Timezone,
    bool EnableActivityLog,
    bool DisplayOnProfile,
    int Prioritize,
    bool LinkUserAdapter
) : ICommand<Guid>;
