using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Members;
using Domain.Plans.Events;
using Domain.Shared;
using Domain.Shared.Exceptions;
using NodaTime;

namespace Domain.Plans;

/// <summary>
/// Aggregate root representing a trackable plan owned by a member.
/// </summary>
public class Plan : AggregateRoot<Guid>
{
    private readonly List<ActivityLog> _activityLogs = [];

    // Required by EF Core
    private Plan()
    {
        Name = default!;
        Unit = default!;
        StartDateLocal = default!;
        EndDateLocal = default!;
        Timezone = default!;
        Status = default!;
    }

    private Plan(
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
        PlanStatus status,
        int likeCount)
    {
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
        Status = status;
        LikeCount = likeCount;
    }

    /// <summary>
    /// Gets the display name of this plan.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the unit of measurement for this plan's target and activity logs.
    /// </summary>
    /// <example>km, steps, hours</example>
    public string Unit { get; private set; }

    /// <summary>
    /// Gets the numeric target this plan aims to reach.
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
    /// Gets whether activity logging is enabled for this plan.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableActivityLog { get; private set; } = true;

    /// <summary>
    /// Gets the current lifecycle status of this plan.
    /// </summary>
    public PlanStatus Status { get; private set; }

    /// <summary>
    /// Gets the total number of likes on this plan.
    /// Denormalized for fast read — incremented and decremented
    /// by the <see cref="Like"/> aggregate via domain events.
    /// </summary>
    public int LikeCount { get; private set; }

    /// <summary>
    /// Gets the sport-specific details if this is a sport plan.
    /// <c>null</c> if this is a generic (non-sport) plan.
    /// </summary>
    public SportPlanDetails? SportPlanDetails { get; private set; }

    /// <summary>
    /// Gets all activity logs recorded against this plan.
    /// </summary>
    public IReadOnlyList<ActivityLog> ActivityLogs => _activityLogs.AsReadOnly();

    /// <summary>
    /// Creates a new generic <see cref="Plan"/>.
    /// </summary>
    public static Plan Create(
        string name,
        string unit,
        float target,
        Instant from,
        Instant to,
        string startDateLocal,
        string endDateLocal,
        string timezone,
        bool enableActivityLog,
        IClock clock,
        Guid createUserId)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        var plan = new Plan(
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
            PlanStatus.Planned,
            likeCount: 0
        )
        {
            Id = Guid.NewGuid(),
        };

        plan.StampCreatedAudit(createUserId, clock);

        var result = plan.Validate();
        if (!result.IsValid)
        {
            throw new DomainValidationException(result);
        }

        plan.RaiseDomainEvent(new PlanCreatedEvent(
            plan.Id,
            name,
            target,
            unit,
            startDateLocal,
            endDateLocal,
            timezone,
            clock,
            $"{nameof(Plan)} / {nameof(Create)}"));

        return plan;
    }

    /// <summary>
    /// Creates a new sport-specific <see cref="Plan"/> for a member.
    /// </summary>
    /// <param name="name">The display name of the plan.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="target">The numeric target. Must be greater than zero.</param>
    /// <param name="from">The UTC instant at which the plan period starts.</param>
    /// <param name="to">The UTC instant at which the plan period ends.</param>
    /// <param name="startDateLocal">The local start date as an ISO string.</param>
    /// <param name="endDateLocal">The local end date as an ISO string.</param>
    /// <param name="timezone">The IANA timezone identifier.</param>
    /// <param name="enableActivityLog">Whether activity logging is enabled.</param>
    /// <param name="sportPlanDetails">
    /// The sport-specific extension details. Must not be null.
    /// </param>
    /// <param name="clock">The system clock.</param>
    /// <returns>A fully validated sport <see cref="Plan"/> instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sportPlanDetails"/> is null.
    /// </exception>
    /// <exception cref="DomainValidationException">
    /// Thrown when any invariant is violated.
    /// </exception>
    public static Plan CreateSportPlan(
        string name,
        string unit,
        float target,
        Instant from,
        Instant to,
        string startDateLocal,
        string endDateLocal,
        string timezone,
        bool enableActivityLog,
        SportPlanDetails sportPlanDetails,
        IClock clock,
        Guid createUserId)
    {
        var plan = Create(
            name,
            unit,
            target,
            from,
            to,
            startDateLocal,
            endDateLocal,
            timezone,
            enableActivityLog,
            clock,
            createUserId);

        plan.SportPlanDetails = sportPlanDetails ?? throw new ArgumentNullException(nameof(sportPlanDetails));
        return plan;
    }

    /// <summary>
    /// Adds an activity log to this plan and recalculates the current value.
    /// </summary>
    /// <param name="value">The recorded value for the activity.</param>
    /// <param name="activityLocalDate">The local date on which the activity was completed.</param>
    /// <param name="externalSource">The provider and external ID if this log originated from an external service.</param>
    /// <param name="clock">The system clock to get the current time.</param>
    /// <param name="createUserId">The ID of the user creating the log.</param>
    /// <returns>The created <see cref="ActivityLog"/> instance.</returns>
    public ActivityLog AddActivityLog(
        float value,
        string activityLocalDate,
        ExternalActivitySource? externalSource,
        IClock clock,
        Guid createUserId)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        var activityLog = ActivityLog.Create(
            Id,
            value,
            activityLocalDate,
            externalSource,
            createUserId,
            clock);

        _activityLogs.Add(activityLog);

        RecalculateCurrentValue();

        // Inform other parts of the system (if any) that an activity log was added.
        // E.g., RaiseDomainEvent - ActivityLogAddedDomainEvent

        return activityLog;
    }

    /// <summary>
    /// Recalculates the current value based on all associated activity logs.
    /// Auto-completes the plan if the target is reached and the plan is active.
    /// </summary>
    private void RecalculateCurrentValue()
    {
        CurrentValue = _activityLogs.Sum(log => log.Value);

        if (Status == PlanStatus.Active && CurrentValue >= Target)
        {
            Status = PlanStatus.Completed;
        }
    }

    /// <summary>
    /// Marks the plan as expired if the current time has passed the plan's end date
    /// and the target has not been met.
    /// </summary>
    /// <param name="clock">The system clock.</param>
    public void MarkAsExpired(IClock clock)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (Status != PlanStatus.Active)
        {
            return;
        }

        if (clock.GetCurrentInstant() > To && CurrentValue < Target)
        {
            Status = PlanStatus.Expired;
        }
    }

    /// <summary>
    /// Updates the plan details.
    /// </summary>
    public void Update(
        string unit,
        float target,
        float current,
        Instant from,
        Instant to,
        Guid byUserId,
        IClock clock)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        Unit = unit;
        Target = target;
        CurrentValue = current;
        From = from;
        To = to;

        StampUpdatedAudit(byUserId, clock);
    }

    /// <summary>
    /// Cancels the plan, setting its status to Cancelled.
    /// Only Active or Planned plans can be cancelled.
    /// </summary>
    /// <param name="byUserId">The ID of the user performing the cancellation.</param>
    /// <param name="clock">The system clock.</param>
    public void Cancel(Guid byUserId, IClock clock)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (Status != PlanStatus.Active && Status != PlanStatus.Planned)
        {
            return;
        }

        Status = PlanStatus.Cancelled;
        StampUpdatedAudit(byUserId, clock);
    }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ValidationError(
                "name", "Plan name is required.", "REQUIRED_NAME"));
        }

        if (string.IsNullOrWhiteSpace(Unit))
        {
            errors.Add(new ValidationError(
                "unit", "Unit is required.", "REQUIRED_UNIT"));
        }

        if (Target <= 0)
        {
            errors.Add(new ValidationError(
                "target", "Target must be greater than zero.", "INVALID_TARGET"));
        }

        if (string.IsNullOrWhiteSpace(StartDateLocal))
        {
            errors.Add(new ValidationError(
                "startDateLocal", "Start date is required.", "REQUIRED_START_DATE"));
        }

        if (string.IsNullOrWhiteSpace(EndDateLocal))
        {
            errors.Add(new ValidationError(
                "endDateLocal", "End date is required.", "REQUIRED_END_DATE"));
        }

        if (!string.IsNullOrWhiteSpace(StartDateLocal)
            && !string.IsNullOrWhiteSpace(EndDateLocal)
            && string.Compare(StartDateLocal, EndDateLocal, StringComparison.Ordinal) >= 0)
        {
            errors.Add(new ValidationError(
                "endDateLocal",
                "End date must be after start date.",
                "INVALID_DATE_RANGE"));
        }

        if (string.IsNullOrWhiteSpace(Timezone))
        {
            errors.Add(new ValidationError(
                "timezone", "Timezone is required.", "REQUIRED_TIMEZONE"));
        }

        if (From >= To)
        {
            errors.Add(new ValidationError(
                "to", "Period end must be after period start.", "INVALID_PERIOD_RANGE"));
        }

        return errors.Count == 0
            ? new ValidationResult(new List<ValidationError>().AsReadOnly())
            : new ValidationResult(new List<ValidationError>(errors).AsReadOnly());
    }
}
