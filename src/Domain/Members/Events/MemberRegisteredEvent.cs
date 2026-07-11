using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Members.Events;

/// <summary>
/// Domain event published when a new member successfully registers in the system.
/// Contains initial profile information such as the starting avatar.
/// </summary>
public sealed class MemberRegisteredEvent(
    Guid memberId,
    string? initialAvatarPath,
    IClock occurredOn,
    string occurredBy) : DomainEvent(occurredOn, occurredBy)
{
    /// <summary>
    /// Gets the identifier of the member who subscribed to the plan.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    public string? InitialAvatarPath { get; } = initialAvatarPath;
}
