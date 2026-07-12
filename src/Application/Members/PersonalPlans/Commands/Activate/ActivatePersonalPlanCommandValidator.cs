using FluentValidation;

namespace Application.Members.PersonalPlans.Commands.Activate;

/// <summary>
/// Validator for the <see cref="ActivatePersonalPlanCommand"/> to ensure all required fields are present
/// before the activation logic is executed.
/// </summary>
public class ActivatePersonalPlanCommandValidator : AbstractValidator<ActivatePersonalPlanCommand>
{
    public ActivatePersonalPlanCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithMessage("IdentifyName is required.");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithMessage("PlanId is required.");
    }
}
