using Application.Shared;

namespace Application.ExternalSync.Commands.RevokeExternalConnectionByExternalUser;

/// <summary>Revokes an active domain connection identified by its provider user identity.</summary>
/// <param name="ProviderId">The external provider identifier.</param>
/// <param name="ExternalUserId">The provider user identifier.</param>
public sealed record RevokeExternalConnectionByExternalUserCommand(
    string ProviderId,
    string ExternalUserId) : ICommand<bool>;
