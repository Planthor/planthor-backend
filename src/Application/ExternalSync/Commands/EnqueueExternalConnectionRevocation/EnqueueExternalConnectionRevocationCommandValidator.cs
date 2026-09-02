using FluentValidation;

namespace Application.ExternalSync.Commands.EnqueueExternalConnectionRevocation;

/// <summary>Validates provider-originated revocation job payloads.</summary>
public sealed class EnqueueExternalConnectionRevocationCommandValidator
    : AbstractValidator<EnqueueExternalConnectionRevocationCommand>
{
    /// <summary>Defines required provider revocation fields.</summary>
    public EnqueueExternalConnectionRevocationCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.ExternalUserId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty();
    }
}
