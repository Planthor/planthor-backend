using System;
using Application.Shared;

namespace Application.Members.Commands.Patch;

/// <summary>
/// Command to patch an existing member's properties using a field mask.
/// </summary>
public record PatchMemberCommand(
    Guid Id,
    string[] UpdateMask,
    string? IdentifyName,
    string? FirstName,
    string? LastName) : ICommand;
