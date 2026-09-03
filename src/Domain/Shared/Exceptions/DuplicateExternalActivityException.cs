using System;

namespace Domain.Shared.Exceptions;

/// <summary>
/// Raised when an external provider activity is added to the same plan more than once.
/// </summary>
/// <remarks>
/// Initializes the exception with the provider activity identity and target plan.
/// </remarks>
/// <param name="providerId">The external provider identifier.</param>
/// <param name="externalActivityId">The provider's activity identifier.</param>
/// <param name="planId">The plan that already contains the activity.</param>
public sealed class DuplicateExternalActivityException(
    string providerId,
    string externalActivityId,
    Guid planId)
    : InvalidOperationException($"External activity '{providerId}/{externalActivityId}' already exists on plan '{planId}'.")
{

    /// <summary>Gets the external provider identifier.</summary>
    public string ProviderId { get; } = providerId;

    /// <summary>Gets the provider's activity identifier.</summary>
    public string ExternalActivityId { get; } = externalActivityId;

    /// <summary>Gets the plan containing the duplicate activity.</summary>
    public Guid PlanId { get; } = planId;
}
