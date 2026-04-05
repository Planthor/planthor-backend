using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Members.Events;

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
