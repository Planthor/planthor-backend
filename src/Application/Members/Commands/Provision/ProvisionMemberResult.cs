using System;

namespace Application.Members.Commands.Provision;

/// <summary>
/// Result of provisioning a member.
/// </summary>
public record ProvisionMemberResult(Guid MemberId, string IdentifyName);
