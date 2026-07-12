using FluentValidation;
using NodaTime;

namespace Application.Members.Commands.Update;

/// <summary>
/// Validates the initial payload required to create a member.
/// </summary>
public class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    private const int MaxFirstNameLength = 100;
    private const int MaxLastNameLength = 100;

    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode("error_first_name_required")
            .MaximumLength(MaxFirstNameLength).WithErrorCode("error_first_name_too_long");

        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode("error_last_name_required")
            .MaximumLength(MaxLastNameLength).WithErrorCode("error_last_name_too_long");

        RuleFor(x => x.PreferredTimezone)
            .NotEmpty().WithErrorCode("error_preferred_timezone_required")
            .Must(tz => !string.IsNullOrEmpty(tz) && DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz) is not null)
            .WithErrorCode("error_preferred_timezone_invalid");
    }
}
