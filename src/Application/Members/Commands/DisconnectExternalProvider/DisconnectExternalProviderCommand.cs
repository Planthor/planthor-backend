using Application.Shared;

namespace Application.Members.Commands.DisconnectExternalProvider;

/// <summary>
/// Command to disconnect an external provider from a member.
/// </summary>
/// <param name="IdentifyName">The identify name of the member.</param>
/// <param name="ProviderId">The provider ID.</param>
/// <param name="ConnectionTypeId">The connection type ID.</param>
public record DisconnectExternalProviderCommand(
    string IdentifyName,
    string ProviderId,
    string ConnectionTypeId) : ICommand;
