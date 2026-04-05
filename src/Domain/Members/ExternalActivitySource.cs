using System.Collections.Generic;
using Domain.Shared;

namespace Domain.Members;

/// <summary>
/// Value object identifying the external origin of an activity log entry.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="ExternalActivitySource"/>.
/// </remarks>
/// <param name="provider">The external provider that produced this activity.</param>
/// <param name="externalActivityId">
/// The unique activity identifier on the external platform
/// (e.g., a Strava activity ID or GitHub commit SHA).
/// </param>
public sealed class ExternalActivitySource(ExternalProvider provider, string externalActivityId) : ValueObject
{
    /// <summary>
    /// Gets the external provider that produced this activity.
    /// </summary>
    public ExternalProvider Provider { get; } = provider;

    /// <summary>
    /// Gets the unique activity identifier on the external platform.
    /// </summary>
    /// <example>1234567890 (Strava activity ID), abc123def (GitHub commit SHA)</example>
    public string ExternalActivityId { get; } = externalActivityId;

    /// <inheritdoc/>
    protected override IEnumerable<object> EqualityComponents
    {
        get
        {
            yield return Provider.Id;
            yield return ExternalActivityId;
        }
    }
}
