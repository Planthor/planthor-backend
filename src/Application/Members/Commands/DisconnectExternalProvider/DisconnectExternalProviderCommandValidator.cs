using System.Linq;
using Domain.Members;
using FluentValidation;

namespace Application.Members.Commands.DisconnectExternalProvider;

/// <summary>
/// Validator for the <see cref="DisconnectExternalProviderCommand"/>.
/// </summary>
public sealed class DisconnectExternalProviderCommandValidator : AbstractValidator<DisconnectExternalProviderCommand>
{
    private const int MaxIdentifyNameLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisconnectExternalProviderCommandValidator"/> class.
    /// </summary>
    public DisconnectExternalProviderCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .MaximumLength(MaxIdentifyNameLength);

        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .Must(id => Enumerable.Any(ExternalProvider.All, p => p.Id == id))
            .WithMessage(cmd => $"'{cmd.ProviderId}' is not a valid External Provider ID.");

        RuleFor(x => x.ConnectionTypeId)
            .NotEmpty()
            .Must(id => Enumerable.Any(ExternalConnectionType.All, t => t.Id == id))
            .WithMessage(cmd => $"'{cmd.ConnectionTypeId}' is not a valid External Connection Type ID.");
    }
}
