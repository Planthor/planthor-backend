using System.Collections.Generic;
using Application.Shared;

namespace Application.Members.Commands.ConnectExternalProvider;

public record ConnectExternalProviderCommand(
    string IdentifyName,
    string ProviderId,
    string ConnectionTypeId,
    string ExternalUserId,
    IReadOnlyList<string> Scopes) : ICommand;
