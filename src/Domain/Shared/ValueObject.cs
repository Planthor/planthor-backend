using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Shared;

/// <summary>
/// Represents a base class for value objects.
/// Value objects are immutable objects that encapsulate a set of attributes or properties.
/// They are used to model concepts that have no identity and are solely defined by their attributes.
/// Value objects promote immutability, encapsulation, and strong typing.
/// They are an essential building block in DDD and Clean Architecture to ensure domain integrity and separation of concerns.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components used to determine equality between
    /// two value object instances.
    /// </summary>
    protected abstract IEnumerable<object> EqualityComponents { get; }

    /// <inheritdoc/>
    public bool Equals(ValueObject other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        var thisComponents = EqualityComponents ?? Enumerable.Empty<object>();
        var otherComponents = other.EqualityComponents ?? Enumerable.Empty<object>();

        return thisComponents.SequenceEqual(
            otherComponents,
            EqualityComparer<object>.Default);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj) =>
        obj is ValueObject other && Equals(other);

    /// <summary>
    /// Returns a hash code derived from all equality components.
    /// </summary>
    /// <remarks>
    /// Uses prime-number multiplication rather than XOR to ensure
    /// that component order matters — preventing collisions between
    /// value objects whose components are permutations of each other.
    /// For example, <c>(A, B)</c> and <c>(B, A)</c> will produce
    /// different hash codes.
    /// </remarks>
    public override int GetHashCode()
    {
        var components = EqualityComponents ?? Enumerable.Empty<object>();

        unchecked
        {
            return components.Aggregate(17, (hash, component) =>
                hash * 31 + (component?.GetHashCode() ?? 0));
        }
    }

    /// <summary>
    /// Determines whether two value objects are structurally equal.
    /// </summary>
    public static bool operator ==(ValueObject left, ValueObject right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two value objects are not structurally equal.
    /// </summary>
    public static bool operator !=(ValueObject left, ValueObject right) =>
        !(left == right);
}
