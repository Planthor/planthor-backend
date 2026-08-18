using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Members.Events;

/// <summary>
/// Domain event published when a new member successfully registers in the system.
/// Contains initial profile information such as the starting avatar.
/// </summary>
/// <param name="memberId">The ID of the new member.</param>
/// <param name="initialAvatarPath">The initial avatar path.</param>
/// <param name="occurredOn">The clock providing the current time.</param>
/// <param name="occurredBy">The user ID triggering the event.</param>
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

    /// <summary>
    /// Gets the initial avatar path for the registered member.
    /// </summary>
    public string? InitialAvatarPath { get; } = initialAvatarPath;
}
