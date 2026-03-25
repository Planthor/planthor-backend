using System;
using System.Collections.Generic;
using PlanthorWebApi.Domain.Shared;
using PlanthorWebApi.Domain.Shared.Goals;

namespace PlanthorWebApi.Domain;

/// <summary>
/// Represents a sport-specific extension of a <see cref="Goal"/>.
/// </summary>
/// <remarks>
/// Modelled as an owned entity (not a separate aggregate) since it has
/// no meaningful existence outside of its parent <see cref="Goal"/>.
/// <para>
/// Assumption: <c>sportType</c> stores Strava sport type identifiers
/// as a list. An empty list means ALL sport types are accepted.
/// </para>
/// </remarks>
public sealed class SportGoalDetails : ValueObject
{
    /// <summary>
    /// Initialises sport goal details accepting all sport types.
    /// </summary>
    public SportGoalDetails() : this("kilometer", []) { }

    /// <summary>
    /// Initialises sport goal details with specific sport types.
    /// </summary>
    public SportGoalDetails(string unit, IReadOnlyList<string> sportTypes)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit must not be empty.", nameof(unit));

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
                yield return type;
        }
    }
}