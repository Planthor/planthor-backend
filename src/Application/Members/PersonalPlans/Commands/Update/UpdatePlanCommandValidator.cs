using FluentValidation;

namespace Application.Members.PersonalPlans.Commands.Update;

/// <summary>
/// Validator for editable plan metadata. Progress remains derived from ActivityLogs.
/// </summary>
public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePersonalPlanCommand>
{
    private const int MinTarget = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePlanCommandValidator"/> class.
    /// </summary>
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithErrorCode("error_identity_name_required");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithErrorCode("error_plan_id_required");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithErrorCode("error_unit_required");

        RuleFor(x => x.Target)
            .GreaterThan(MinTarget)
            .WithErrorCode("error_target_invalid");

        RuleFor(x => x.ToDate)
            .GreaterThan(x => x.FromDate)
            .WithErrorCode("error_todate_before_fromdate");
    }
}
