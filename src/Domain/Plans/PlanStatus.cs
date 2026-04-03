using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Plans;

public class PlanStatus
{
    /// <summary>
    /// A Plan that have not been started.
    /// </summary>
    public static readonly PlanStatus Planned = new("P", "PLANNED", "PlanStatus_Planned_Desc");

    /// <summary>
    /// An active Plan.
    /// </summary>
    public static readonly PlanStatus Active = new("A", "Active", "PlanStatus_Active_Desc");

    /// <summary>
    /// The Plan already exceed the deadline.
    /// </summary>
    public static readonly PlanStatus Exceeded = new("E", "EXCEEDED", "PlanStatus_Exceeded_Desc");

    /// <summary>
    /// The plan has been closed.
    /// </summary>
    public static readonly PlanStatus Closed = new("C", "CLOSED", "PlanStatus_Closed_Desc");

    /// <summary>
    /// Gets the unique short-hand identifier for the Plan Type (e.g., "P", "Q", "E").
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the uppercase display name of the Plan Type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the localization key used by the i18n service to fetch the translated description.
    /// </summary>
    public string I18NKey { get; }

    private PlanStatus(string id, string name, string descriptionI18nKey)
    {
        Id = id;
        Name = name;
        I18NKey = descriptionI18nKey;
    }

    /// <summary>
    /// Retrieves a <see cref="PlanStatus"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The short-hand identifier (e.g., "E").</param>
    /// <returns>The matching <see cref="PlanStatus"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid type.</exception>
    public static PlanStatus FromId(string id)
    {
        return All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid PeriodType identifier.");
    }

    /// <summary>
    /// Returns a collection of all available <see cref="PeriodType"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<PlanStatus> All => [Planned, Active, Exceeded, Closed];
}
