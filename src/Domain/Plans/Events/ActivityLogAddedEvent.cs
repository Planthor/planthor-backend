using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Plans.Events;

/// <summary>
/// Raised when an <see cref="ActivityLog"/> is successfully appended to a <see cref="Plan"/>.
/// </summary>
/// <remarks>
/// Consumers can react to this event to update read models, send notifications,
/// or trigger downstream analytics without coupling to the Plan aggregate's write path.
/// </remarks>
/// <param name="planId">The identifier of the plan that received the log.</param>
/// <param name="activityLogId">The identifier of the newly created activity log.</param>
/// <param name="value">The numeric value recorded by the activity log.</param>
/// <param name="newCurrentValue">The plan's updated <see cref="Plan.CurrentValue"/> after the log was added.</param>
/// <param name="clock">The system clock used to timestamp the event.</param>
/// <param name="occurredBy">The entity or operation that triggered the event.</param>
public sealed class ActivityLogAddedEvent(
    Guid planId,
    Guid activityLogId,
    float value,
    float newCurrentValue,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the identifier of the plan that received the activity log.
    /// </summary>
    public Guid PlanId { get; } = planId;

    /// <summary>
    /// Gets the identifier of the newly created activity log.
    /// </summary>
    public Guid ActivityLogId { get; } = activityLogId;

    /// <summary>
    /// Gets the numeric value recorded by the activity log.
    /// </summary>
    public float Value { get; } = value;

    /// <summary>
    /// Gets the plan's updated current value after the log was added.
    /// </summary>
    public float NewCurrentValue { get; } = newCurrentValue;
}
