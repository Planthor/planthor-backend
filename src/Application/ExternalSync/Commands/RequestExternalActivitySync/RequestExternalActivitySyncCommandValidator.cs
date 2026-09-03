using FluentValidation;

namespace Application.ExternalSync.Commands.RequestExternalActivitySync;

/// <summary>Validates authenticated manual activity sync requests.</summary>
public sealed class RequestExternalActivitySyncCommandValidator
    : AbstractValidator<RequestExternalActivitySyncCommand>
{
    /// <summary>Defines required member and provider identifiers.</summary>
    public RequestExternalActivitySyncCommandValidator()
    {
        RuleFor(command => command.IdentifyName).NotEmpty();
        RuleFor(command => command.ProviderId).NotEmpty();
    }
}
