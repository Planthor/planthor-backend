using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Shared;

/// <summary>
/// Well-known provider-neutral activity sync trigger names.
/// </summary>
public sealed class ExternalActivitySyncTrigger : IEquatable<ExternalActivitySyncTrigger>
{
    /// <summary>The first historical import after connection.</summary>
    public static readonly ExternalActivitySyncTrigger Initial = new("initial");

    /// <summary>An authenticated member-requested import.</summary>
    public static readonly ExternalActivitySyncTrigger Manual = new("manual");

    /// <summary>A single provider webhook activity.</summary>
    public static readonly ExternalActivitySyncTrigger Webhook = new("webhook");

    /// <summary>A deferred retry after quota or transient failure.</summary>
    public static readonly ExternalActivitySyncTrigger Retry = new("retry");

    /// <summary>Gets the name of the trigger.</summary>
    public string Name { get; }

    private ExternalActivitySyncTrigger(string name)
    {
        Name = name;
    }

    /// <summary>Retrieves a trigger by its name.</summary>
    public static ExternalActivitySyncTrigger FromName(string name)
    {
        return All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{name}' is not a valid ExternalActivitySyncTrigger name.");
    }

    /// <summary>All known triggers.</summary>
    public static IReadOnlyCollection<ExternalActivitySyncTrigger> All => [Initial, Manual, Webhook, Retry];

    /// <summary>Implicitly converts the trigger to its string name.</summary>
    public static implicit operator string(ExternalActivitySyncTrigger trigger) => trigger.Name;
    
    /// <summary>Implicitly converts a string name to its corresponding trigger.</summary>
    public static implicit operator ExternalActivitySyncTrigger(string name) => FromName(name);

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ExternalActivitySyncTrigger);

    /// <inheritdoc />
    public bool Equals(ExternalActivitySyncTrigger? other) => other is not null && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() => Name.GetHashCode(StringComparison.OrdinalIgnoreCase);

    /// <summary>Compares two triggers for equality.</summary>
    public static bool operator ==(ExternalActivitySyncTrigger? left, ExternalActivitySyncTrigger? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>Compares two triggers for inequality.</summary>
    public static bool operator !=(ExternalActivitySyncTrigger? left, ExternalActivitySyncTrigger? right) => !(left == right);
}
