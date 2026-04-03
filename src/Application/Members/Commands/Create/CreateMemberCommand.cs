using System;
using Application.Shared;

namespace Application.Members.Commands.Create;

/// <summary>
/// Command to create a new member.
/// </summary>
/// <param name="FirstName">The first name of the member.</param>
/// <param name="MiddleName">The middle name of the member.</param>
/// <param name="LastName">The last name of the member.</param>
/// <param name="Description">A free-text description or bio of the member.</param>
/// <param name="PreferredTimezone">The IANA timezone identifier preferred by the member (e.g., "Asia/Ho_Chi_Minh").</param>
public record CreateMemberCommand(
    string IdentifyName,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Description,
    string PreferredTimezone) : ICommand<Guid>;
