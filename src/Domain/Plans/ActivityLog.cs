using System;
using Domain.Members;
using Domain.Shared;
using NodaTime;

namespace Domain.Plans;

public sealed class ActivityLog : IEntity<Guid>, IHasAudit
{
    private ActivityLog() { }

    /// <inheritdoc/>
    public Guid Id { get; private set; }

    /// <summary>Gets the ID of the plan this log belongs to.</summary>
    public Guid PlanId { get; private set; }

    /// <summary>
    /// Gets the recorded value in the plan's unit of measurement.
    /// </summary>
    public float Value { get; private set; }

    /// <summary>
    /// Gets the local date on which this activity was completed.
    /// Stored as ISO string for timezone-safe boundary comparisons.
    /// </summary>
    /// <example>2026-06-15</example>
    public string ActivityLocalDate { get; private set; } = default!;

    /// <summary>
    /// Gets the UTC instant at which this activity was completed.
    /// Defaults to <see cref="CreatedAt"/> if not provided.
    /// </summary>
    public Instant CompletedDate { get; private set; }

    /// <summary>
    /// Gets the provider and external ID of the source activity,
    /// if this log originated from an external service.
    /// <c>null</c> for manually recorded entries.
    /// </summary>
    public ExternalActivitySource? ExternalSource { get; private set; }

    /// <inheritdoc/>
    public Instant CreatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid CreatedBy { get; private set; }

    /// <inheritdoc/>
    public Instant LastUpdatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid LastUpdatedBy { get; private set; }

    internal static ActivityLog Create(
        Guid planId,
        float value,
        string activityLocalDate,
        ExternalActivitySource? externalActivitySource,
        Guid createdBy,
        IClock clock)
    {
        var now = clock.GetCurrentInstant();

        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Value = value,
            ActivityLocalDate = activityLocalDate,
            CompletedDate = now,
            ExternalSource = externalActivitySource,
            CreatedAt = now,
            CreatedBy = createdBy,
            LastUpdatedAt = now,
            LastUpdatedBy = createdBy
        };
    }
}
