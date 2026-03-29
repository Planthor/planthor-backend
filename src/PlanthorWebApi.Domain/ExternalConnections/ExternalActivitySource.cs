using System.Collections.Generic;
using PlanthorWebApi.Domain.Shared;

namespace PlanthorWebApi.Domain.ExternalConnections;

/// <summary>
/// Value object identifying the external origin of an activity log entry.
/// </summary>
public sealed class ExternalActivitySource : ValueObject
{
    /// <summary>
    /// Initializes a new <see cref="ExternalActivitySource"/>.
    /// </summary>
    /// <param name="provider">The external provider that produced this activity.</param>
    /// <param name="externalActivityId">
    /// The unique activity identifier on the external platform
    /// (e.g., a Strava activity ID or GitHub commit SHA).
    /// </param>
    public ExternalActivitySource(ExternalProvider provider, string externalActivityId)
    {
        Provider = provider;
        ExternalActivityId = externalActivityId;
    }

    /// <summary>
    /// Gets the external provider that produced this activity.
    /// </summary>
    public ExternalProvider Provider { get; }

    /// <summary>
    /// Gets the unique activity identifier on the external platform.
    /// </summary>
    /// <example>1234567890 (Strava activity ID), abc123def (GitHub commit SHA)</example>
    public string ExternalActivityId { get; }

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
