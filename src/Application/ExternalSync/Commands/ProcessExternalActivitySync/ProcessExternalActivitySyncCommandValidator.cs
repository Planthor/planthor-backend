using FluentValidation;

namespace Application.ExternalSync.Commands.ProcessExternalActivitySync;

/// <summary>Validates scheduler activity-sync payloads before processing.</summary>
public sealed class ProcessExternalActivitySyncCommandValidator
    : AbstractValidator<ProcessExternalActivitySyncCommand>
{
    /// <summary>Defines required provider and athlete job fields.</summary>
    public ProcessExternalActivitySyncCommandValidator()
    {
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.ProviderId).NotEmpty();
        RuleFor(command => command.Request.ExternalUserId).NotEmpty();
        RuleFor(command => command.Request.Trigger).NotEmpty();
    }
}
