using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Members.Events;

/// <summary>
///
/// </summary>
/// <param name="memberId"></param>
/// <param name="oldAvatarUri"></param>
/// <param name="newAvatarUri"></param>
/// <param name="clock"></param>
/// <param name="occurredBy"></param>
public sealed class MemberAvatarUpdatedEvent(
    Guid memberId,
    Uri oldAvatarUri,
    Uri newAvatarUri,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the identifier of the member who subscribed to the plan.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    public Uri OldAvatarUri { get; } = oldAvatarUri;

    public Uri NewAvatarUri { get; } = newAvatarUri;
}
