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
            .WithErrorCode("error_identity_name_required");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithErrorCode("error_plan_id_required");
    }
}
