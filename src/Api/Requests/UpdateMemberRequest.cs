namespace Api.Requests;

/// <summary>
/// Request model to update an existing member.
/// </summary>
/// <param name="FirstName">The first name of the member.</param>
/// <param name="MiddleName">The middle name of the member.</param>
/// <param name="LastName">The last name of the member.</param>
/// <param name="Description">A free-text description or bio of the member.</param>
/// <param name="PathAvatar">Path or URL to the member's avatar image.</param>
/// <param name="PreferredTimezone">The IANA timezone identifier preferred by the member.</param>
public record UpdateMemberRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Description,
    string? PathAvatar,
    string PreferredTimezone);
