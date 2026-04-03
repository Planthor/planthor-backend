using FluentValidation;
using NodaTime;

namespace Application.Members.Commands.Create;

/// <summary>
/// Validates the initial payload required to create a member.
/// </summary>
public class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty().WithMessage("Identity name is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.PreferredTimezone)
            .NotEmpty().WithMessage("Preferred timezone is required.")
            .Must(tz => !string.IsNullOrEmpty(tz) && DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz) is not null)
            .WithMessage("Preferred timezone must be a valid IANA timezone identifier.");
    }
}
