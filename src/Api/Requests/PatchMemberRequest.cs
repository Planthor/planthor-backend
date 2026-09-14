using System.ComponentModel.DataAnnotations;

namespace Api.Requests;

/// <summary>
/// Request model to patch an existing member's properties using a field mask pattern.
/// </summary>
/// <remarks>IdentifyName is assigned during provisioning and cannot be included in UpdateMask.</remarks>
/// <param name="UpdateMask">An array of field names that should be updated.</param>
/// <param name="FirstName">The new first name.</param>
/// <param name="LastName">The new last name.</param>
/// <param name="AutoLinkUserAdapterToPlan">Whether to auto link user adapter to plan.</param>
public record PatchMemberRequest(
    [Required] string[] UpdateMask,
    string? FirstName,
    string? LastName,
    bool? AutoLinkUserAdapterToPlan);
