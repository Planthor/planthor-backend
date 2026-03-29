using System;

namespace PlanthorWebApi.Application.Dtos;

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
public record PersonalPlanDto(
    Guid PlanId,
    Guid MemberId,
    bool DisplayOnProfile,
    int Prioritize,
    bool LinkUserAdapters
);
