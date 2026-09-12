using System;
using Application.Shared;

namespace Application.Members.Commands.Update;

/// <summary>
/// Command to update an existing member's properties.
/// </summary>
/// <param name="Id">The unique identifier of the member to update.</param>
/// <param name="FirstName">The first name of the member.</param>
/// <param name="MiddleName">The middle name of the member.</param>
/// <param name="LastName">The last name of the member.</param>
/// <param name="Description">The description or bio of the member.</param>
/// <param name="PathAvatar">The path or URL to the member's avatar image.</param>
/// <param name="PreferredTimezone">The preferred timezone of the member.</param>
/// <param name="AutoLinkUserAdapterToPlan">Whether to automatically link the user adapter to a plan.</param>
public record UpdateMemberCommand(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Description,
    string? PathAvatar,
    string PreferredTimezone,
    bool AutoLinkUserAdapterToPlan) : ICommand;
