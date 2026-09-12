namespace Api.Requests;

/// <summary>
/// Request model to create a new member.
/// </summary>
/// <param name="FirstName">The first name of the member.</param>
/// <param name="MiddleName">The middle name of the member.</param>
/// <param name="LastName">The last name of the member.</param>
/// <param name="Description">A free-text description or bio of the member.</param>
/// <param name="PreferredTimezone">The IANA timezone identifier preferred by the member (e.g., "Asia/Ho_Chi_Minh").</param>
/// <param name="AutoLinkUserAdapterToPlan">Whether to auto link user adapter to plan.</param>
public record CreateMemberRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Description,
    string PreferredTimezone,
    bool AutoLinkUserAdapterToPlan);
