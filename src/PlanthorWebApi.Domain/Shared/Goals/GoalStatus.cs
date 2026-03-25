using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanthorWebApi.Domain.Shared.Goals;

public class GoalStatus
{
    /// <summary>
    /// A Goal that have not been started.
    /// </summary>
    public static readonly GoalStatus Planned = new("P", "PLANNED", "GoalStatus_Planned_Desc");

    /// <summary>
    /// An active Goal.
    /// </summary>
    public static readonly GoalStatus Active = new("A", "Active", "GoalStatus_Active_Desc");

    /// <summary>
    /// The Goal already exceed the deadline.
    /// </summary>
    public static readonly GoalStatus Exceeded = new("E", "EXCEEDED", "GoalStatus_Exceeded_Desc");

    /// <summary>
    /// The goal has been closed.
    /// </summary>
    public static readonly GoalStatus Closed = new("C", "CLOSED", "GoalStatus_Closed_Desc");

    /// <summary>
    /// Gets the unique short-hand identifier for the Goal Type (e.g., "P", "Q", "E").
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the uppercase display name of the Goal Type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the localization key used by the i18n service to fetch the translated description.
    /// </summary>
    public string DescriptionI18nKey { get; }

    private GoalStatus(string id, string name, string descriptionI18nKey)
    {
        Id = id;
        Name = name;
        DescriptionI18nKey = descriptionI18nKey;
    }

    /// <summary>
    /// Retrieves a <see cref="GoalStatus"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The short-hand identifier (e.g., "E").</param>
    /// <returns>The matching <see cref="GoalStatus"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid type.</exception>
    public static GoalStatus FromId(string id)
    {
        return All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid PeriodType identifier.");
    }

    /// <summary>
    /// Returns a collection of all available <see cref="PeriodType"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<GoalStatus> All => [Planned, Active, Exceeded, Closed];
}