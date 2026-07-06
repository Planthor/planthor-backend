using System;

namespace Application.Dtos;

/// <summary>
/// Data transfer object representing a member.
/// </summary>
/// <param name="Id">The unique identifier of the member.</param>
/// <param name="FirstName">The first name of the member.</param>
/// <param name="MiddleName">The middle name of the member, if any.</param>
/// <param name="LastName">The last name of the member.</param>
/// <param name="Description">A brief personal description or bio of the member.</param>
/// <param name="PathAvatar">The URL or relative path to the member's avatar image.</param>
public record MemberDto(
    Guid Id,
    string FirstName,
    string MiddleName,
    string LastName,
    string? Description,
    string PathAvatar);
