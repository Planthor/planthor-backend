namespace Api.Requests;

using System.Text.Json.Serialization;

/// <summary>
/// The HTTP request body for creating a new activity log.
/// </summary>
/// <param name="Value">The recorded value for the activity (e.g., distance, duration) in the plan's specific unit.</param>
/// <param name="ActivityLocalDate">The local date on which the activity was completed, formatted as an ISO string (e.g. YYYY-MM-DD).</param>
/// <param name="ExternalProviderId">The ID of the external provider (e.g., STRAVA, GITHUB).</param>
/// <param name="ExternalActivityId">The unique ID of the activity on the external platform.</param>
public record CreateActivityLogRequest(
    [property: JsonRequired] float Value,
    string ActivityLocalDate,
    string? ExternalProviderId = null,
    string? ExternalActivityId = null
);
