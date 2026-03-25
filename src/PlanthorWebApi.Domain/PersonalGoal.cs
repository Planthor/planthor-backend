using System;
using System.Collections.Generic;
using PlanthorWebApi.Domain.Shared;

namespace PlanthorWebApi.Domain;

/// <summary>
/// Aggregate root representing a member's personal relationship to a goal.
/// </summary>
/// <remarks>
/// Acts as a join aggregate between <c>Member</c> and <c>Goal</c>,
/// enriched with member-specific goal preferences such as display order,
/// profile visibility, and Strava sync opt-in.
/// </remarks>
public class PersonalGoal : AggregateRoot<Guid>
{
    private PersonalGoal() { }

    /// <summary>Gets the ID of the member who owns this personal goal.</summary>
    public Guid MemberId { get; private set; }

    /// <summary>Gets the ID of the underlying goal.</summary>
    public Guid GoalId { get; private set; }

    /// <summary>
    /// Gets whether this goal is displayed on the member's public profile.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool DisplayOnProfile { get; private set; } = true;

    /// <summary>
    /// Gets the display priority of this goal on the member's profile.
    /// Lower values indicate higher priority. Range: 0–99.
    /// </summary>
    public int Prioritize { get; private set; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        if (MemberId == Guid.Empty)
        {
            errors.Add(new ValidationError(
                "memberId", "Member ID is required.", "REQUIRED_MEMBER_ID"));
        }

        if (GoalId == Guid.Empty)
        {
            errors.Add(new ValidationError(
                "goalId", "Goal ID is required.", "REQUIRED_GOAL_ID"));
        }

        if (Prioritize is < 0 or > 99)
        {
            errors.Add(new ValidationError(
                "prioritize", "Priority must be between 0 and 99.", "INVALID_PRIORITY"));
        }
        
        return errors.Count == 0
            ? new ValidationResult(new List<ValidationError>().AsReadOnly())
            : new ValidationResult(new List<ValidationError>(errors).AsReadOnly());
    }
}
