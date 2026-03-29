using System;
using NodaTime;
using PlanthorWebApi.Domain.Shared;

namespace PlanthorWebApi.Domain.Plans.Events;

/// <summary>
/// Initializes a new instance of <see cref="PlanCreatedEvent"/>.
/// </summary>
/// <param name="planId">The identifier of the newly created plan.</param>
/// <param name="memberId">The identifier of the member who owns the plan.</param>
/// <param name="planName">The display name of the plan.</param>
/// <param name="target">The numeric target of the plan.</param>
/// <param name="unit">The unit of measurement.</param>
/// <param name="startDateLocal">The local start date as an ISO string.</param>
/// <param name="endDateLocal">The local end date as an ISO string.</param>
/// <param name="timezone">The IANA timezone identifier.</param>
/// <param name="clock"> The system clock used to timestamp when this event occurred.</param>
public sealed class PlanCreatedEvent(
    Guid planId,
    Guid memberId,
    string planName,
    float target,
    string unit,
    string startDateLocal,
    string endDateLocal,
    string timezone,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the unique identifier of the plan that was created.
    /// </summary>
    public Guid PlanId { get; } = planId;

    /// <summary>
    /// Gets the identifier of the member who owns this plan.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the name of the plan.
    /// Included so consumers do not need to re-fetch the aggregate
    /// for simple notification or logging purposes.
    /// </summary>
    public string PlanName { get; } = planName;

    /// <summary>
    /// Gets the numeric target this plan aims to reach.
    /// </summary>
    public float Target { get; } = target;

    /// <summary>
    /// Gets the unit of measurement for this plan.
    /// </summary>
    public string Unit { get; } = unit;

    /// <summary>
    /// Gets the local start date of the plan period as an ISO date string.
    /// </summary>
    /// <example>2026-01-01</example>
    public string StartDateLocal { get; } = startDateLocal;

    /// <summary>
    /// Gets the local end date of the plan period as an ISO date string.
    /// </summary>
    /// <example>2026-12-31</example>
    public string EndDateLocal { get; } = endDateLocal;

    /// <summary>
    /// Gets the IANA timezone identifier snapshotted at plan creation time.
    /// </summary>
    /// <example>Asia/Ho_Chi_Minh</example>
    public string Timezone { get; } = timezone;
}
