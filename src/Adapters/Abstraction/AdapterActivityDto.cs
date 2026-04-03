using NodaTime;

namespace Adapters.Abstraction;

/// <summary>
/// Normalized activity returned by any <see cref="IActivitySyncAdapter"/>.
/// Maps to <c>ActivityLog</c> in the domain.
/// </summary>
public record AdapterActivityDto(
    string ExternalActivityId,      // Strava activity ID, GitHub commit SHA…
    string ProviderId,              // "STRAVA" | "GITHUB"
    string Name,                    // Activity name / commit message
    Instant OccurredAt,             // When the activity happened (UTC)
    string? ActivityType,           // "Run", "Ride", "commit" (raw provider value)
    double? DistanceMeters,         // null for non-fitness activities
    Duration? MovingTime            // null for non-fitness activities
);
