using System;
using NodaTime;

namespace Domain.Shared;

public interface IHasAudit
{
    /// <summary>
    /// Gets the UTC instant at which this entity was first created.
    /// </summary>
    Instant CreatedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who created this entity.
    /// </summary>
    Guid CreatedBy { get; }

    /// <summary>
    /// Gets the UTC instant at which this entity was most recently modified.
    /// </summary>
    Instant LastUpdatedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified this entity.
    /// </summary>
    Guid LastUpdatedBy { get; }
}
