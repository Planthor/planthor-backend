using System;
using Backend.Domain.Shared;
using NodaTime;

namespace Backend.Domain.Members.Events;

/// <summary>
/// Domain event raised when a member successfully subscribes to a plan,
/// creating a new <see cref="PersonalPlan"/> entity within the member aggregate.
/// </summary>
/// <param name="memberId">
/// The identifier of the member who subscribed.
/// </param>
/// <param name="personalPlanId">
/// The identifier of the newly created <see cref="PersonalPlan"/> entity.
/// </param>
/// <param name="planId">
/// The identifier of the underlying <c>Plan</c> aggregate being linked.
/// </param>
/// <param name="displayOnProfile">
/// Whether this plan should appear on the member's public profile.
/// </param>
/// <param name="prioritize">
/// The display priority on the member's profile. Range: 0–999.
/// </param>
/// <param name="linkUserAdapter">
/// Whether Strava activity sync is enabled for this personal plan.
/// </param>
/// <param name="clock">
/// The system clock used to timestamp when this event occurred.
/// </param>
/// <param name="occurredBy">
/// The component identifier that caused the event to be raised.
/// </param>
public sealed class MemberSubscribedToPlanEvent(
    Guid memberId,
    Guid personalPlanId,
    Guid planId,
    bool displayOnProfile,
    int prioritize,
    bool linkUserAdapter,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the identifier of the member who subscribed to the plan.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the identifier of the newly created <see cref="PersonalPlan"/> entity.
    /// </summary>
    public Guid PersonalPlanId { get; } = personalPlanId;

    /// <summary>
    /// Gets the identifier of the underlying <c>Plan</c> aggregate that was linked.
    /// </summary>
    public Guid PlanId { get; } = planId;

    /// <summary>
    /// Gets whether this personal plan will be displayed on the member's public profile.
    /// </summary>
    public bool DisplayOnProfile { get; } = displayOnProfile;

    /// <summary>
    /// Gets the display priority of this plan on the member's profile.
    /// Lower values indicate higher priority. Range: 0–999.
    /// </summary>
    public int Prioritize { get; } = prioritize;

    /// <summary>
    /// Gets whether Strava activity sync is enabled for this personal plan.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the Strava sync handler should begin attributing
    /// incoming activities to this plan if they fall within the plan's
    /// period boundary and sport type filters.
    /// </remarks>
    public bool LinkUserAdapter { get; } = linkUserAdapter;
}
