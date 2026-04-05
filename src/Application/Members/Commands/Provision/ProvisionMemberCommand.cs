using System;
using Application.Shared;

namespace Application.Members.Commands.Provision;

/// <summary>
/// Provisioning Member support Create member for JIT authentication.
/// </summary>
/// <param name="IdentifyName"></param>
/// <param name="FirstName"></param>
/// <param name="LastName"></param>
/// <param name="AvatarUrl"></param>
public record ProvisionMemberCommand(
    string IdentifyName,
    string FirstName,
    string LastName,
    Uri? AvatarUrl) : ICommand<Guid>;
