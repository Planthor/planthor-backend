using System;
using Domain.ExternalConnections;
using Domain.Shared;
using NodaTime;

namespace Domain.Members.Events;

/// <summary>
/// Domain event raised when a member establishes a new connection to an external
/// service provider, or reactivates a previously revoked connection.
/// </summary>
/// <param name="memberId">The identifier of the member who established the connection.</param>
/// <param name="externalConnectionId">The identifier of the newly created or reactivated connection.</param>
/// <param name="provider">The external service provider that was connected.</param>
/// <param name="externalUserId">The member's identifier on the external platform.</param>
/// <param name="clock">The system clock used to timestamp when this event occurred.</param>
/// <param name="occurredBy">The name or identifier of the component that raised this event.</param>
public sealed class ExternalConnectionEstablishedEvent(
    Guid memberId,
    Guid externalConnectionId,
    ExternalProvider provider,
    string externalUserId,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the identifier of the member who established the connection.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the identifier of the external connection entity.
    /// </summary>
    public Guid ExternalConnectionId { get; } = externalConnectionId;

    /// <summary>
    /// Gets the external service provider that was connected.
    /// </summary>
    public ExternalProvider Provider { get; } = provider;

    /// <summary>
    /// Gets the member's identifier on the external platform.
    /// </summary>
    public string ExternalUserId { get; } = externalUserId;
}
