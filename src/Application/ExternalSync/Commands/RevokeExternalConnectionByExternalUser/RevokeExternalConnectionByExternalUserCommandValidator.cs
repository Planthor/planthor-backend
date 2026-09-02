using FluentValidation;

namespace Application.ExternalSync.Commands.RevokeExternalConnectionByExternalUser;

/// <summary>Validates provider-originated domain revocation commands.</summary>
public sealed class RevokeExternalConnectionByExternalUserCommandValidator
    : AbstractValidator<RevokeExternalConnectionByExternalUserCommand>
{
    /// <summary>Defines required provider revocation identifiers.</summary>
    public RevokeExternalConnectionByExternalUserCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.ExternalUserId).NotEmpty();
    }
}
