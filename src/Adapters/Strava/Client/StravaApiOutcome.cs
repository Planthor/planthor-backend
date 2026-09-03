namespace Adapters.Strava.Client;

/// <summary>
/// Describes a Strava API operation outcome as a strongly-typed smart enum, 
/// avoiding converting failures into empty activity pages.
/// </summary>
public sealed class StravaApiOutcome
{
    /// <summary>The request succeeded.</summary>
    public static readonly StravaApiOutcome Success = new("SUCCESS", "Success");

    /// <summary>The requested resource does not exist.</summary>
    public static readonly StravaApiOutcome NotFound = new("NOT_FOUND", "NotFound");

    /// <summary>The access or refresh token is no longer usable.</summary>
    public static readonly StravaApiOutcome AuthorizationRequired = new("AUTHORIZATION_REQUIRED", "AuthorizationRequired");

    /// <summary>Application read quota is exhausted or reserved.</summary>
    public static readonly StravaApiOutcome RateLimited = new("RATE_LIMITED", "RateLimited");

    /// <summary>A temporary network or upstream failure occurred.</summary>
    public static readonly StravaApiOutcome TransientFailure = new("TRANSIENT_FAILURE", "TransientFailure");

    /// <summary>Gets the unique identifier for the outcome.</summary>
    public string Id { get; }

    /// <summary>Gets the name of the outcome.</summary>
    public string Name { get; }

    private StravaApiOutcome(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Retrieves a <see cref="StravaApiOutcome"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The short-hand identifier.</param>
    /// <returns>The matching <see cref="StravaApiOutcome"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid outcome.</exception>
    public static StravaApiOutcome FromId(string id)
    {
        return All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid StravaApiOutcome identifier.");
    }

    /// <summary>
    /// Returns a collection of all available <see cref="StravaApiOutcome"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<StravaApiOutcome> All => [Success, NotFound, AuthorizationRequired, RateLimited, TransientFailure];
}
