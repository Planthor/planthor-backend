using System;

namespace Application.Dtos;

/// <summary>
/// Data transfer object representing an activity log.
/// </summary>
/// <param name="Id">The unique identifier of the activity log.</param>
/// <param name="PlanId">The unique identifier of the plan this activity log belongs to.</param>
/// <param name="Value">The recorded value for the activity.</param>
/// <param name="ActivityLocalDate">The local date on which the activity was completed.</param>
/// <param name="CompletedDate">The UTC date and time when the activity was completed.</param>
/// <param name="ExternalSourceProvider">The provider name if this log originated from an external service (e.g., Strava).</param>
/// <param name="ExternalSourceId">The external ID from the provider if this log originated from an external service.</param>
public record ActivityLogDto(
    Guid Id,
    Guid PlanId,
    float Value,
    string ActivityLocalDate,
    DateTimeOffset CompletedDate,
    string? ExternalSourceProvider = null,
    string? ExternalSourceId = null
);
