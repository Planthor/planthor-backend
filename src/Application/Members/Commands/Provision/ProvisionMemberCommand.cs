using System;
using Application.Shared;

namespace Application.Members.Commands.Provision;

/// <summary>
/// Provisioning Member support Create member for JIT authentication.
/// </summary>
/// <param name="SubjectId">The validated identity provider subject used to resolve the member's external connection.</param>
/// <param name="IdentifyName">The exact preferred username to store when creating a member.</param>
/// <param name="FirstName">The given name from the authenticated identity.</param>
/// <param name="LastName">The family name from the authenticated identity.</param>
/// <param name="AvatarUrl">An optional avatar to download when the member does not already have one.</param>
public record ProvisionMemberCommand(
    string SubjectId,
    string IdentifyName,
    string FirstName,
    string LastName,
    Uri? AvatarUrl) : ICommand<ProvisionMemberResult>;
