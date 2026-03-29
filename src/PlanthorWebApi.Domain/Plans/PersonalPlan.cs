using System;
using System.Collections.Generic;
using NodaTime;
using PlanthorWebApi.Domain.Plans.Events;
using PlanthorWebApi.Domain.Shared;
using PlanthorWebApi.Domain.Shared.Exceptions;

namespace PlanthorWebApi.Domain.Plans;

/// <summary>
/// Aggregate root representing a member's personal relationship to a plan.
/// </summary>
/// <remarks>
/// Acts as a join aggregate between <c>Member</c> and <c>Plan</c>,
/// enriched with member-specific plan preferences such as display order,
/// profile visibility, and Strava sync opt-in.
/// </remarks>
public class PersonalPlan(
    Guid memberId,
    Guid planId,
    bool displayOnProfile,
    int prioritize,
    bool linkUserAdapter
    ) : AggregateRoot<Guid>
{
    private const int MaxPrioritize = 999;

    /// <summary>
    /// Gets the ID of the member who owns this personal plan.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the ID of the underlying plan.
    /// </summary>
    public Guid PlanId { get; } = planId;

    /// <summary>
    /// Gets whether this plan is displayed on the member's public profile.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool DisplayOnProfile { get; } = displayOnProfile;

    /// <summary>
    /// Gets the display priority of this plan on the member's profile.
    /// Lower values indicate higher priority. Range: 0–99.
    /// </summary>
    public int Prioritize { get; } = prioritize;

    /// <summary>
    /// Gets whether Strava activity sync is enabled for this plan.
    /// When <c>true</c>, incoming Strava activities are automatically
    /// recorded as activity logs against this plan if they fall within
    /// the plan's period boundary and sport type filters.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool LinkUserAdapter { get; } = linkUserAdapter;

    public static PersonalPlan Create(
        Guid memberId,
        Guid planId,
        bool displayOnProfile,
        int prioritize,
        bool linkUserAdapter,
        Guid createdBy,
        IClock clock)
    {
        var personalPlan = new PersonalPlan(
            memberId,
            planId,
            displayOnProfile,
            prioritize,
            linkUserAdapter
        )
        {
            Id = Guid.NewGuid()
        };

        personalPlan.StampCreatedAudit(createdBy, clock);

        var result = personalPlan.Validate();

        if (!result.IsValid)
        {
            throw new DomainValidationException(result);
        }

        personalPlan.RaiseDomainEvent(
            new PersonalPlanCreatedEvent(
                personalPlan.Id,
                memberId,
                planId,
                displayOnProfile,
                prioritize,
                linkUserAdapter,
                clock));

        return personalPlan;
    }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        if (MemberId == Guid.Empty)
        {
            errors.Add(new ValidationError(
                "memberId", "Member ID is required.", "REQUIRED_MEMBER_ID"));
        }

        if (PlanId == Guid.Empty)
        {
            errors.Add(new ValidationError(
                "planId", "Plan ID is required.", "REQUIRED_GOAL_ID"));
        }

        if (Prioritize is < 0 or > MaxPrioritize)
        {
            errors.Add(new ValidationError(
                "prioritize", "Priority must be between 0 and 99.", "INVALID_PRIORITY"));
        }

        return errors.Count == 0
            ? new ValidationResult(new List<ValidationError>().AsReadOnly())
            : new ValidationResult(new List<ValidationError>(errors).AsReadOnly());
    }
}
