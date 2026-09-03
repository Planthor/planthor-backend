using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Dtos;

/// <summary>
/// Describes a provider-neutral result returned from an activity adapter.
/// </summary>
public sealed class ActivitySyncOutcome
{
    /// <summary>The provider operation completed successfully.</summary>
    public static readonly ActivitySyncOutcome Success = new("S", "SUCCESS");

    /// <summary>The requested provider activity no longer exists.</summary>
    public static readonly ActivitySyncOutcome NotFound = new("N", "NOT_FOUND");

    /// <summary>The provider authorization is missing, expired, or revoked.</summary>
    public static readonly ActivitySyncOutcome AuthorizationRequired = new("A", "AUTHORIZATION_REQUIRED");

    /// <summary>The provider quota requires work to resume at a later instant.</summary>
    public static readonly ActivitySyncOutcome RateLimited = new("R", "RATE_LIMITED");

    /// <summary>A temporary provider or network failure should be retried.</summary>
    public static readonly ActivitySyncOutcome TransientFailure = new("T", "TRANSIENT_FAILURE");

    /// <summary>
    /// Gets the unique short-hand identifier for the outcome.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the uppercase display name of the outcome.
    /// </summary>
    public string Name { get; }

    private ActivitySyncOutcome() : this(default!, default!) { }

    private ActivitySyncOutcome(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Retrieves a <see cref="ActivitySyncOutcome"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The short-hand identifier.</param>
    /// <returns>The matching <see cref="ActivitySyncOutcome"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid outcome.</exception>
    public static ActivitySyncOutcome FromId(string id)
    {
        return All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid ActivitySyncOutcome identifier.");
    }

    /// <summary>
    /// Returns a collection of all available <see cref="ActivitySyncOutcome"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<ActivitySyncOutcome> All => [Success, NotFound, AuthorizationRequired, RateLimited, TransientFailure];
}
