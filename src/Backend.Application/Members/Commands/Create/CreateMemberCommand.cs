using System;
using Backend.Application.Shared;

namespace Backend.Application.Members.Commands.Create;

/// <summary>
/// 
/// </summary>
/// <param name="IdentifyName"></param>
/// <param name="FirstName"></param>
/// <param name="MiddleName"></param>
/// <param name="LastName"></param>
/// <param name="Description"></param>
/// <param name="PathAvatar"></param>
public record CreateMemberCommand(
    string IdentifyName,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Description,
    string? PathAvatar) : ICommand<Guid>;
