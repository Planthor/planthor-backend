using System;
using Application.Shared;

namespace Application.Members.Commands.Update;

/// <summary>
/// Command to update a member's avatar path.
/// </summary>
/// <param name="MemberId">The unique identifier of the member.</param>
/// <param name="AvatarPath">The local path or URL to the stored avatar.</param>
public record UpdateMemberAvatarCommand(Guid MemberId, string AvatarPath) : ICommand;
