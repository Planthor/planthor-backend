using FluentValidation;

namespace Application.Members.PersonalPlans.Commands.Cancel;

/// <summary>
/// Validator for the <see cref="CancelPlanCommand"/> to ensure all required fields are present
/// before the cancellation logic is executed.
/// </summary>
public class CancelPlanCommandValidator : AbstractValidator<CancelPlanCommand>
{
    public CancelPlanCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithMessage("IdentifyName is required.");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithMessage("PlanId is required.");
    }
}
