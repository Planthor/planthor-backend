using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Members.Events;

/// <summary>
/// Domain event raised when a member revokes a connection to an external service provider.
/// </summary>
/// <param name="memberId">The identifier of the member who revoked the connection.</param>
/// <param name="externalConnectionId">The identifier of the revoked connection.</param>
/// <param name="provider">The external service provider that was disconnected.</param>
/// <param name="clock">The system clock used to timestamp when this event occurred.</param>
/// <param name="occurredBy">The name or identifier of the component that raised this event.</param>
public sealed class ExternalConnectionRevokedEvent(
    Guid memberId,
    Guid externalConnectionId,
    ExternalProvider provider,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the identifier of the member who revoked the connection.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the identifier of the revoked external connection entity.
    /// </summary>
    public Guid ExternalConnectionId { get; } = externalConnectionId;

    /// <summary>
    /// Gets the external service provider that was disconnected.
    /// </summary>
    public ExternalProvider Provider { get; } = provider;
}
