using System;

namespace Application.Dtos;

/// <summary>
/// Data transfer object representing a member's personalized view and configuration of a specific plan.
/// </summary>
/// <param name="PlanId">The unique identifier of the underlying global plan.</param>
/// <param name="MemberId">The unique identifier of the member who owns this personal plan record.</param>
/// <param name="DisplayOnProfile">Indicates if this plan should be visible on the member's public-facing profile.</param>
/// <param name="Prioritize">
/// The display priority on the member's profile.
/// Lower values indicate higher priority. Valid range is 0 to 999.
/// </param>
/// <param name="LinkUserAdapters">
/// Indicates if external integrations (e.g., Strava) are enabled to automatically
/// sync activity data to this specific plan.
/// </param>
/// <param name="PlanName">The name of the plan.</param>
/// <param name="Unit">The unit of measurement for the plan (e.g., km, hours, times).</param>
/// <param name="Target">The numeric target to achieve for the plan.</param>
/// <param name="CurrentValue">The current accumulated value for the plan.</param>
/// <param name="ProgressPercentage">The percentage of completion for the plan.</param>
/// <param name="Completed">Indicates whether the plan's target has been met.</param>
/// <param name="Status">The current status of the plan (e.g., Active, Expired, Completed).</param>
/// <param name="FromDate">The UTC start date and time of the plan.</param>
/// <param name="ToDate">The UTC end date and time of the plan.</param>
public record PersonalPlanDto(
    Guid PlanId,
    Guid MemberId,
    bool DisplayOnProfile,
    int Prioritize,
    bool LinkUserAdapters,
    string PlanName,
    string Unit,
    float Target,
    float CurrentValue,
    double ProgressPercentage,
    bool Completed,
    string Status,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null
);
