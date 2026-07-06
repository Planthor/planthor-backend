using System;
using Application.Shared;

using Domain.Plans;
using Domain.Members;

namespace Application.Members.ActivityLogs.Commands.Create;

/// <summary>
/// Command to create a new activity log entry for a specific plan.
/// </summary>
/// <param name="PlanId">The unique identifier of the plan this activity log belongs to.</param>
/// <param name="Value">The recorded value for the activity (e.g., distance, duration) in the plan's specific unit.</param>
/// <param name="ActivityLocalDate">The local date on which the activity was completed, formatted as an ISO string (e.g. YYYY-MM-DD).</param>
/// <param name="IdentifyName">The identity name of the member creating the log.</param>
/// <param name="ExternalSource">The external source details if the log is from an integration like Strava.</param>
public record CreateActivityLogCommand(
    Guid PlanId,
    float Value,
    string ActivityLocalDate,
    string IdentifyName = "",
    ExternalActivitySource? ExternalSource = null
) : ICommand<Guid>;
