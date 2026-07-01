using System;
using FluentValidation;
using NodaTime;

namespace Application.Members.PersonalPlans.Commands.Create;

/// <summary>
/// Validates the payload required to create a new plan and subscribe a member to it.
/// </summary>
public class CreatePersonalPlanCommandValidator : AbstractValidator<CreatePersonalPlanCommand>
{
    public CreatePersonalPlanCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty().WithMessage("Identify name is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Plan name is required.")
            .MaximumLength(100).WithMessage("Plan name must not exceed 100 characters.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required.")
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.");

        RuleFor(x => x.Target)
            .GreaterThan(0).WithMessage("Target must be greater than zero.");

        RuleFor(x => x.ToDate)
            .GreaterThan(x => x.FromDate)
            .WithMessage("ToDate must be after FromDate.");

        RuleFor(x => x.StartDateLocal)
            .NotEmpty().WithMessage("Start date local is required.");

        RuleFor(x => x.EndDateLocal)
            .NotEmpty().WithMessage("End date local is required.")
            .Must((cmd, endDate) => string.Compare(cmd.StartDateLocal, endDate, StringComparison.Ordinal) <= 0)
            .When(cmd => !string.IsNullOrEmpty(cmd.StartDateLocal) && !string.IsNullOrEmpty(cmd.EndDateLocal))
            .WithMessage("End date local must be on or after start date local.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .Must(tz => !string.IsNullOrEmpty(tz) && DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz) is not null)
            .WithMessage("Timezone must be a valid IANA timezone identifier.");

        RuleFor(x => x.Prioritize)
            .InclusiveBetween(0, 999).WithMessage("Priority must be between 0 and 999.");
    }
}
