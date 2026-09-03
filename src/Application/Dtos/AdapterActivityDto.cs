using NodaTime;

namespace Application.Dtos;

/// <summary>
/// Provider-neutral activity data normalized by an external adapter.
/// </summary>
/// <param name="ExternalActivityId">The provider's immutable activity identifier.</param>
/// <param name="ProviderId">The Planthor external-provider identifier.</param>
/// <param name="CanonicalSportTypeId">The canonical Planthor sport identifier.</param>
/// <param name="OccurredAt">The instant at which the athlete performed the activity.</param>
/// <param name="DistanceMeters">The positive SI distance, or <c>null</c> when unavailable.</param>
public sealed record AdapterActivityDto(
    string ExternalActivityId,
    string ProviderId,
    string CanonicalSportTypeId,
    Instant OccurredAt,
    double? DistanceMeters);
