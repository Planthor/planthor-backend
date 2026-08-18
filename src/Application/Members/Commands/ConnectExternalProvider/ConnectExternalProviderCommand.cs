using System.Collections.Generic;
using Application.Shared;

namespace Application.Members.Commands.ConnectExternalProvider;

/// <summary>
/// Command to connect an external provider to a member.
/// </summary>
/// <param name="IdentifyName">The identify name of the member.</param>
/// <param name="ProviderId">The provider ID.</param>
/// <param name="ConnectionTypeId">The connection type ID.</param>
/// <param name="ExternalUserId">The external user ID.</param>
/// <param name="Scopes">The scopes granted.</param>
public record ConnectExternalProviderCommand(
    string IdentifyName,
    string ProviderId,
    string ConnectionTypeId,
    string ExternalUserId,
    IReadOnlyList<string> Scopes) : ICommand;
