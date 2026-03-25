using System;
using System.Collections.Generic;
using NodaTime;
using PlanthorWebApi.Domain.Shared.Exceptions;
using PlanthorWebApi.Domain.Shared.Goals.Events;

namespace PlanthorWebApi.Domain.Shared.Goals;

/// <summary>
/// Aggregate root representing a trackable goal owned by a member.
/// </summary>
public class Goal : AggregateRoot<Guid>
{
    private readonly List<ActivityLog> _activityLogs = [];

    private Goal(
        Guid memberId,
        string name,
        string unit,
        float target,
        float currentValue,
        Instant from,
        Instant to,
        string startDateLocal,
        string endDateLocal,
        string timezone,
        bool enableActivityLog,
        bool completed,
        GoalStatus status,
        int likeCount
        )
    {
        MemberId = memberId;
        Name = name;
        Unit = unit;
        Target = target;
        CurrentValue = currentValue;
        From = from;
        To = to;
        StartDateLocal = startDateLocal;
        EndDateLocal = endDateLocal;
        Timezone = timezone;
        EnableActivityLog = enableActivityLog;
        Completed = completed;
        Status = status;
        LikeCount = likeCount;
    }

    /// <summary>
    /// Gets the identifier of the member who owns this goal.
    /// A goal always belongs to exactly one member.
    /// </summary>
    public Guid MemberId { get; private set; }

    /// <summary>
    /// Gets the display name of this goal.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the unit of measurement for this goal's target and activity logs.
    /// </summary>
    /// <example>km, steps, hours</example>
    public string Unit { get; private set; }

    /// <summary>
    /// Gets the numeric target this goal aims to reach.
    /// </summary>
    public float Target { get; private set; }

    /// <summary>
    /// Gets the current aggregate value across all activity logs.
    /// Compared against <see cref="Target"/> to determine completion.
    /// </summary>
    public float CurrentValue { get; private set; }

    /// <summary>
    /// Gets the UTC instant at which this period starts.
    /// </summary>
    public Instant From { get; private set; }

    /// <summary>
    /// Gets the UTC instant at which this period ends.
    /// </summary>
    public Instant To { get; private set; }

    /// <summary>
    /// Gets the local start date as an ISO string.
    /// Used for boundary comparisons against activity local dates.
    /// </summary>
    public string StartDateLocal { get; private set; }

    /// <summary>
    /// Gets the local end date as an ISO string.
    /// Used for boundary comparisons against activity local dates.
    /// </summary>
    public string EndDateLocal { get; private set; }

    /// <summary>
    /// Gets the IANA timezone identifier snapshotted at creation time.
    /// </summary>
    public string Timezone { get; private set; }

    /// <summary>
    /// Gets whether this goal has been completed.
    /// A goal is auto-completed when <see cref="CurrentValue"/> meets
    /// or exceeds <see cref="Target"/>.
    /// </summary>
    public bool Completed { get; private set; }

    /// <summary>
    /// Gets whether activity logging is enabled for this goal.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableActivityLog { get; private set; } = true;

    /// <summary>
    /// Gets the current lifecycle status of this goal.
    /// </summary>
    public GoalStatus Status { get; private set; }

    /// <summary>
    /// Gets the total number of likes on this goal.
    /// Denormalised for fast read — incremented and decremented
    /// by the <see cref="Like"/> aggregate via domain events.
    /// </summary>
    public int LikeCount { get; private set; }

    /// <summary>
    /// Gets the sport-specific details if this is a sport goal.
    /// <c>null</c> if this is a generic (non-sport) goal.
    /// </summary>
    public SportGoalDetails? SportGoalDetails { get; private set; }

    /// <summary>
    /// Gets all activity logs recorded against this goal.
    /// </summary>
    public IReadOnlyList<ActivityLog> ActivityLogs => _activityLogs.AsReadOnly();

    /// <summary>
    /// Creates a new generic <see cref="Goal"/>.
    /// </summary>
    public static Goal Create(
        Guid memberId,
        string name,
        string unit,
        float target,
        Instant from,
        Instant to,
        string startDateLocal,
        string endDateLocal,
        string timezone,
        bool enableActivityLog,
        IClock clock)
    {
        var goal = new Goal(
            memberId,
            name,
            unit,
            target,
            currentValue: 0f,
            from,
            to,
            startDateLocal,
            endDateLocal,
            timezone,
            enableActivityLog,
            completed: false,
            GoalStatus.Planned,
            likeCount: 0
        )
        {
            Id = Guid.NewGuid(),
        };

        goal.StampCreatedAudit(memberId, clock);

        var result = goal.Validate();
        if (!result.IsValid)
            throw new DomainValidationException(result);

        goal.RaiseDomainEvent(new GoalCreatedEvent(
            goal.Id,
            memberId,
            name,
            target,
            unit,
            startDateLocal,
            endDateLocal,
            timezone,
            clock));

        return goal;
    }

    /// <summary>
    /// Creates a new sport-specific <see cref="Goal"/> for a member.
    /// </summary>
    /// <param name="memberId">The identifier of the member who owns this goal.</param>
    /// <param name="name">The display name of the goal.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="target">The numeric target. Must be greater than zero.</param>
    /// <param name="from">The UTC instant at which the goal period starts.</param>
    /// <param name="to">The UTC instant at which the goal period ends.</param>
    /// <param name="startDateLocal">The local start date as an ISO string.</param>
    /// <param name="endDateLocal">The local end date as an ISO string.</param>
    /// <param name="timezone">The IANA timezone identifier.</param>
    /// <param name="enableActivityLog">Whether activity logging is enabled.</param>
    /// <param name="sportGoalDetails">
    /// The sport-specific extension details. Must not be null.
    /// </param>
    /// <param name="clock">The system clock.</param>
    /// <returns>A fully validated sport <see cref="Goal"/> instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sportGoalDetails"/> is null.
    /// </exception>
    /// <exception cref="DomainValidationException">
    /// Thrown when any invariant is violated.
    /// </exception>
    public static Goal CreateSportGoal(
        Guid memberId,
        string name,
        string unit,
        float target,
        Instant from,
        Instant to,
        string startDateLocal,
        string endDateLocal,
        string timezone,
        bool enableActivityLog,
        SportGoalDetails sportGoalDetails,
        IClock clock)
    {
        var goal = Create(
            memberId, name, unit, target,
            from, to, startDateLocal, endDateLocal, timezone,
            enableActivityLog, clock);

        goal.SportGoalDetails = sportGoalDetails ?? throw new ArgumentNullException(nameof(sportGoalDetails));
        return goal;
    }

    public override ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        if (MemberId == Guid.Empty)
            errors.Add(new ValidationError(
                "memberId", "Member ID is required.", "REQUIRED_MEMBER_ID"));

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError(
                "name", "Goal name is required.", "REQUIRED_NAME"));

        if (string.IsNullOrWhiteSpace(Unit))
            errors.Add(new ValidationError(
                "unit", "Unit is required.", "REQUIRED_UNIT"));

        if (Target <= 0)
            errors.Add(new ValidationError(
                "target", "Target must be greater than zero.", "INVALID_TARGET"));

        if (string.IsNullOrWhiteSpace(StartDateLocal))
            errors.Add(new ValidationError(
                "startDateLocal", "Start date is required.", "REQUIRED_START_DATE"));

        if (string.IsNullOrWhiteSpace(EndDateLocal))
            errors.Add(new ValidationError(
                "endDateLocal", "End date is required.", "REQUIRED_END_DATE"));

        if (!string.IsNullOrWhiteSpace(StartDateLocal)
            && !string.IsNullOrWhiteSpace(EndDateLocal)
            && string.Compare(StartDateLocal, EndDateLocal, StringComparison.Ordinal) >= 0)
            errors.Add(new ValidationError(
                "endDateLocal",
                "End date must be after start date.",
                "INVALID_DATE_RANGE"));

        if (string.IsNullOrWhiteSpace(Timezone))
            errors.Add(new ValidationError(
                "timezone", "Timezone is required.", "REQUIRED_TIMEZONE"));

        if (From >= To)
            errors.Add(new ValidationError(
                "to", "Period end must be after period start.", "INVALID_PERIOD_RANGE"));

        return errors.Count == 0
            ? new ValidationResult(new List<ValidationError>().AsReadOnly())
            : new ValidationResult(new List<ValidationError>(errors).AsReadOnly());
    }
}
