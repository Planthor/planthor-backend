namespace Domain.Shared;

/// <summary>
/// Represents an entity with an identity in the Planthor domain.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Major Code Smell",
    "S3246:Generic type parameters should be co-variant when possible",
    Justification = "TId is intentionally invariant. Covariance is suppressed to " +
                    "prevent unsafe cross-type identity assignments and to future-proof " +
                    "against TId appearing in input positions such as SameIdentityAs(TId).")]
public interface IEntity<TId> where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    TId Id { get; }
}
