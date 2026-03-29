using System;
using System.Collections.Generic;
using PlanthorWebApi.Domain.Shared;

namespace PlanthorWebApi.Domain.Plans;

/// <summary>
/// Represents a sport-specific extension of a <see cref="Plan"/>.
/// </summary>
/// <remarks>
/// Modelled as an owned entity (not a separate aggregate) since it has
/// no meaningful existence outside of its parent <see cref="Plan"/>.
/// <para>
/// Assumption: <c>sportType</c> stores Strava sport type identifiers
/// as a list. An empty list means ALL sport types are accepted.
/// </para>
/// </remarks>
public sealed class SportPlanDetails : ValueObject
{
    /// <summary>
    /// Initializes sport plan details accepting all sport types.
    /// </summary>
    public SportPlanDetails() : this("km", []) { }

    /// <summary>
    /// Initializes sport plan details with specific sport types.
    /// </summary>
    public SportPlanDetails(string unit, IReadOnlyList<string> sportTypes)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit must not be empty.", nameof(unit));
        }

        Unit = unit;
        SportTypes = sportTypes ?? new List<string>().AsReadOnly();
    }


    /// <summary>
    /// Gets the unit of measurement. Defaults to <c>kilometer</c>.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Gets the list of accepted Strava sport type identifiers.
    /// An empty list means all sport types are accepted.
    /// </summary>
    public IReadOnlyList<string> SportTypes { get; }


    /// <inheritdoc/>
    protected override IEnumerable<object> EqualityComponents
    {
        get
        {
            yield return Unit;
            foreach (var type in SportTypes)
            {
                yield return type;
            }
        }
    }
}
