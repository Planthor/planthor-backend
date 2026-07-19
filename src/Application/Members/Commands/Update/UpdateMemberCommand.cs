using System;
using Application.Shared;

namespace Application.Members.Commands.Update;

/// <summary>
///
/// </summary>
/// <param name="FirstName"></param>
/// <param name="MiddleName"></param>
/// <param name="LastName"></param>
/// <param name="Description"></param>
/// <param name="PathAvatar"></param>
public record UpdateMemberCommand(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Description,
    string? PathAvatar,
    string PreferredTimezone) : ICommand;
