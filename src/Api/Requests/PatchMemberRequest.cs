using System.ComponentModel.DataAnnotations;

namespace Api.Requests;

/// <summary>
/// Request model to patch an existing member's properties using a field mask pattern.
/// </summary>
/// <param name="UpdateMask">An array of field names that should be updated.</param>
/// <param name="IdentifyName">The new user-friendly identify name (handle).</param>
/// <param name="FirstName">The new first name.</param>
/// <param name="LastName">The new last name.</param>
public record PatchMemberRequest(
    [Required] string[] UpdateMask,
    string? IdentifyName,
    string? FirstName,
    string? LastName);
