using FluentValidation;

namespace Application.Members.PersonalPlans.Commands.Cancel;

/// <summary>
/// Validator for the <see cref="CancelPlanCommand"/> to ensure all required fields are present
/// before the cancellation logic is executed.
/// </summary>
public sealed class CancelPlanCommandValidator : AbstractValidator<CancelPlanCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelPlanCommandValidator"/> class.
    /// </summary>
    public CancelPlanCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithErrorCode("error_identity_name_required");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithErrorCode("error_plan_id_required");
    }
}
