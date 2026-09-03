using System;
using System.Collections.Generic;
using Domain.Shared;

namespace Domain.Plans;

/// <summary>
/// Represents a sport-specific extension of a <see cref="Plan"/>.
/// </summary>
/// <remarks>
/// Modelled as an owned entity (not a separate aggregate) since it has
/// no meaningful existence outside of its parent <see cref="Plan"/>.
/// <para>
/// <see cref="SportTypes"/> stores canonical Planthor sport-type identifiers.
/// The explicit <c>ALL</c> identifier represents the wildcard selection.
/// </para>
/// </remarks>
public sealed class SportPlanDetails : ValueObject
{
    /// <summary>
    /// Initializes sport plan details with the default unit and no selected sport types.
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
        SportTypes = sportTypes is null ? [] : [.. sportTypes];
    }


    /// <summary>
    /// Gets the unit of measurement. Defaults to <c>kilometer</c>.
    /// </summary>
    public string Unit { get; private set; }

    /// <summary>
    /// Gets the canonical Planthor sport-type identifiers accepted by the plan.
    /// Use <c>ALL</c> as the only entry to represent every supported sport type.
    /// </summary>
    public IReadOnlyList<string> SportTypes { get; private set; }


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
