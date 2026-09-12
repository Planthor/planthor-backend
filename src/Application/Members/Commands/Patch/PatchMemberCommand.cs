using System;
using Application.Shared;

namespace Application.Members.Commands.Patch;

/// <summary>
/// Command to patch an existing member's properties using a field mask.
/// </summary>
/// <param name="Id">The unique identifier of the member to update.</param>
/// <param name="UpdateMask">The fields to update.</param>
/// <param name="IdentifyName">The identification name of the member.</param>
/// <param name="FirstName">The first name of the member.</param>
/// <param name="LastName">The last name of the member.</param>
/// <param name="AutoLinkUserAdapterToPlan">Whether to automatically link the user adapter to a plan.</param>
public record PatchMemberCommand(
    Guid Id,
    string[] UpdateMask,
    string? IdentifyName,
    string? FirstName,
    string? LastName,
    bool? AutoLinkUserAdapterToPlan) : ICommand;
