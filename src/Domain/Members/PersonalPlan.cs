using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Members;

/// <summary>
/// Entity representing a member's personal relationship to a plan.
/// </summary>
/// <remarks>
/// Owned by the <see cref="Member"/> aggregate root. A personal plan
/// has no meaningful existence outside of its parent member.
/// <para>
/// Invariant: A member may not subscribe to the same plan more than once.
/// This invariant is enforced by <see cref="Member"/>, not here.
/// </para>
/// Mutation methods are <c>internal</c> to enforce that all state changes
/// flow through the <see cref="Member"/> aggregate boundary.
/// </remarks>
public sealed class PersonalPlan : IEntity<Guid>, IHasAudit
{
    private const int MaxPrioritize = 999;

    private PersonalPlan() { }

    /// <inheritdoc/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the member who owns this personal plan.
    /// </summary>
    public Guid MemberId { get; private set; }

    /// <summary>
    /// Gets the identifier of the underlying plan being linked.
    /// </summary>
    public Guid PlanId { get; private set; }

    /// <summary>
    /// Gets whether this plan is displayed on the member's public profile.
    /// </summary>
    public bool DisplayOnProfile { get; private set; }

    /// <summary>
    /// Gets the display priority of this plan on the member's profile.
    /// Lower values indicate higher priority. Range: 0–999.
    /// </summary>
    public int Prioritize { get; private set; }

    /// <summary>
    /// Gets whether Strava activity sync is enabled for this plan.
    /// When <c>true</c>, incoming Strava activities are automatically
    /// recorded as activity logs against this plan if they fall within
    /// the plan's period boundary and sport type filters.
    /// </summary>
    public bool LinkUserAdapter { get; private set; }

    /// <inheritdoc/>
    public Instant CreatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid CreatedBy { get; private set; }

    /// <inheritdoc/>
    public Instant LastUpdatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid LastUpdatedBy { get; private set; }

    /// <summary>
    /// Creates a new <see cref="PersonalPlan"/> entity linking a member to a plan.
    /// </summary>
    /// <param name="memberId">The identifier of the member subscribing to the plan.</param>
    /// <param name="planId">The identifier of the plan being linked.</param>
    /// <param name="displayOnProfile">Whether the plan appears on the member's public profile.</param>
    /// <param name="prioritize">Display priority on the member's profile. Range: 0–999.</param>
    /// <param name="linkUserAdapter">Whether Strava activity sync is enabled for this plan.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <returns>A new <see cref="PersonalPlan"/> entity.</returns>
    internal static PersonalPlan Create(
        Guid memberId,
        Guid planId,
        bool displayOnProfile,
        int prioritize,
        bool linkUserAdapter,
        IClock clock)
    {
        if (prioritize is < 0 or > MaxPrioritize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(prioritize),
                $"Priority must be between 0 and {MaxPrioritize}.");
        }

        var now = clock.GetCurrentInstant();

        return new PersonalPlan
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            PlanId = planId,
            DisplayOnProfile = displayOnProfile,
            Prioritize = prioritize,
            LinkUserAdapter = linkUserAdapter,
            CreatedAt = now,
            CreatedBy = memberId,
            LastUpdatedAt = now,
            LastUpdatedBy = memberId
        };
    }

    /// <summary>
    /// Updates the display and sync preferences for this personal plan.
    /// </summary>
    /// <param name="displayOnProfile">Whether the plan should appear on the member's public profile.</param>
    /// <param name="prioritize">The new display priority. Range: 0–999.</param>
    /// <param name="linkUserAdapter">Whether Strava activity sync should remain enabled.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    internal void UpdatePreferences(
        bool displayOnProfile,
        int prioritize,
        bool linkUserAdapter,
        IClock clock)
    {
        if (prioritize is < 0 or > MaxPrioritize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(prioritize),
                $"Priority must be between 0 and {MaxPrioritize}.");
        }

        DisplayOnProfile = displayOnProfile;
        Prioritize = prioritize;
        LinkUserAdapter = linkUserAdapter;
        LastUpdatedAt = clock.GetCurrentInstant();
        LastUpdatedBy = MemberId;
    }
}
