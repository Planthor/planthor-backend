using System;
using Domain.Shared;
using NodaTime;

namespace Domain.Members.Events;

/// <summary>
/// Domain event raised when a member's avatar is updated.
/// </summary>
/// <param name="memberId">The unique identifier of the member whose avatar was updated.</param>
/// <param name="oldAvatarUri">The previous avatar URI, or null if there was no previous avatar.</param>
/// <param name="newAvatarUri">The new avatar URI.</param>
/// <param name="clock">The system clock providing the event occurrence time.</param>
/// <param name="occurredBy">The identity or source that triggered the update.</param>
public sealed class MemberAvatarUpdatedEvent(
    Guid memberId,
    Uri? oldAvatarUri,
    Uri newAvatarUri,
    IClock clock,
    string occurredBy) : DomainEvent(clock, occurredBy)
{
    /// <summary>
    /// Gets the unique identifier of the member whose avatar was updated.
    /// </summary>
    public Guid MemberId { get; } = memberId;

    /// <summary>
    /// Gets the previous avatar URI, or null if there was no previous avatar.
    /// </summary>
    public Uri? OldAvatarUri { get; } = oldAvatarUri;

    /// <summary>
    /// Gets the new avatar URI.
    /// </summary>
    public Uri NewAvatarUri { get; } = newAvatarUri;
}
