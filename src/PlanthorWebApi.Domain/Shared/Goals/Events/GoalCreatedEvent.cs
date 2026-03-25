using System;
using NodaTime;

namespace PlanthorWebApi.Domain.Shared.Goals.Events;

/// <summary>
/// Initialises a new instance of <see cref="GoalCreatedEvent"/>.
/// </summary>
/// <param name="goalId">The identifier of the newly created goal.</param>
/// <param name="memberId">The identifier of the member who owns the goal.</param>
/// <param name="goalName">The display name of the goal.</param>
/// <param name="target">The numeric target of the goal.</param>
/// <param name="unit">The unit of measurement.</param>
/// <param name="startDateLocal">The local start date as an ISO string.</param>
/// <param name="endDateLocal">The local end date as an ISO string.</param>
/// <param name="timezone">The IANA timezone identifier.</param>
/// <param name="clock">
/// The system clock used to timestamp when this event occurred.
/// </param>
public sealed class GoalCreatedEvent(
    Guid goalId,
    Guid memberId,
    string goalName,
    float target,
    string unit,
    string startDateLocal,
    string endDateLocal,
    string timezone,
    IClock clock) : DomainEvent(clock)
{

    /// <summary>
    /// Gets the unique identifier of the goal that was created.
    /// </summary>
    public Guid GoalId { get; } = goalId;

    /// <summary>
    /// Gets the identifier of the member who owns this goal.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the name of the goal.
    /// Included so consumers do not need to re-fetch the aggregate
    /// for simple notification or logging purposes.
    /// </summary>
    public string GoalName { get; } = goalName;

    /// <summary>
    /// Gets the numeric target this goal aims to reach.
    /// </summary>
    public float Target { get; } = target;

    /// <summary>
    /// Gets the unit of measurement for this goal.
    /// </summary>
    public string Unit { get; } = unit;

    /// <summary>
    /// Gets the local start date of the goal period as an ISO date string.
    /// </summary>
    /// <example>2026-01-01</example>
    public string StartDateLocal { get; } = startDateLocal;

    /// <summary>
    /// Gets the local end date of the goal period as an ISO date string.
    /// </summary>
    /// <example>2026-12-31</example>
    public string EndDateLocal { get; } = endDateLocal;

    /// <summary>
    /// Gets the IANA timezone identifier snapshotted at goal creation time.
    /// </summary>
    /// <example>Asia/Ho_Chi_Minh</example>
    public string Timezone { get; } = timezone;
}