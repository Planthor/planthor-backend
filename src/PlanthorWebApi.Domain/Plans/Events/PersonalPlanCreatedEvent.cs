using System;
using NodaTime;
using PlanthorWebApi.Domain.Shared;

namespace PlanthorWebApi.Domain.Plans.Events;

/// <summary>
/// Domain event raised when a member successfully links a plan
/// to their personal profile via a new <see cref="PersonalPlan"/>.
/// </summary>
/// <param name="personalPlanId">
/// The identifier of the newly created <see cref="PersonalPlan"/> aggregate.
/// </param>
/// <param name="memberId">
/// The identifier of the member who owns this personal plan.
/// </param>
/// <param name="planId">
/// The identifier of the underlying <c>Plan</c> aggregate being linked.
/// </param>
/// <param name="displayOnProfile">
/// Whether this plan should appear on the member's public profile.
/// </param>
/// <param name="prioritize">
/// The display priority on the member's profile. Range: 0–99.
/// </param>
/// <param name="linkUserAdapters">
/// Whether Strava activity sync is enabled for this personal plan.
/// </param>
/// <param name="clock">
/// The system clock used to timestamp when this event occurred.
/// </param>
/// <param name="occurredBy">
/// Gets the component identifier that makes the event occurred.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="clock"/> is null.
/// </exception>
public sealed class PersonalPlanCreatedEvent(
    Guid personalPlanId,
    Guid memberId,
    Guid planId,
    bool displayOnProfile,
    int prioritize,
    bool linkUserAdapters,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the unique identifier of the <see cref="PersonalPlan"/>
    /// aggregate that was created.
    /// </summary>
    public Guid PersonalPlanId { get; } = personalPlanId;

    /// <summary>
    /// Gets the identifier of the member who claimed this plan
    /// as their personal plan.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the identifier of the underlying <c>Plan</c> aggregate
    /// that this personal plan is linked to.
    /// </summary>
    public Guid PlanId { get; } = planId;

    /// <summary>
    /// Gets whether this personal plan will be displayed on the
    /// member's public profile.
    /// </summary>
    /// <remarks>
    /// Included so the profile handler can act immediately without
    /// re-fetching the aggregate to check visibility preference.
    /// </remarks>
    public bool DisplayOnProfile { get; } = displayOnProfile;

    /// <summary>
    /// Gets the display priority of this plan on the member's profile.
    /// Lower values indicate higher priority. Range: 0–99.
    /// </summary>
    public int Prioritize { get; } = prioritize;

    /// <summary>
    /// Gets whether Strava activity sync is enabled for this personal plan.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the Strava sync handler should begin attributing
    /// incoming Strava activities to this plan if they fall within the
    /// plan's period boundary and sport type filters.
    /// When <c>false</c>, no automatic attribution occurs.
    /// </remarks>
    public bool LinkUserAdapters { get; } = linkUserAdapters;
}
