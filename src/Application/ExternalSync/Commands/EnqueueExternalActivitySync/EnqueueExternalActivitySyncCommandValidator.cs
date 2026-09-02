using FluentValidation;

namespace Application.ExternalSync.Commands.EnqueueExternalActivitySync;

/// <summary>Validates direct provider activity job requests.</summary>
public sealed class EnqueueExternalActivitySyncCommandValidator
    : AbstractValidator<EnqueueExternalActivitySyncCommand>
{
    /// <summary>Defines the primitive job-payload validation rules.</summary>
    public EnqueueExternalActivitySyncCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.ExternalUserId).NotEmpty();
        RuleFor(command => command.Trigger).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty();
    }
}
