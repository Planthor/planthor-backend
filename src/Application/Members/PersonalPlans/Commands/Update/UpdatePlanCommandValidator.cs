using FluentValidation;

namespace Application.Members.PersonalPlans.Commands.Update;

// TODO - Trung: Revisit when verify details.
/// <summary>
/// Validator for the <see cref="UpdatePersonalPlanCommand"/> to ensure all properties like target, current progress, and date ranges are valid.
/// </summary>
public class UpdatePlanCommandValidator : AbstractValidator<UpdatePersonalPlanCommand>
{
    private const int MinTarget = 0;
    private const int MinCurrent = 0;

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

        RuleFor(x => x.Current)
            .GreaterThanOrEqualTo(MinCurrent)
            .WithErrorCode("error_current_invalid");

        RuleFor(x => x.ToDate)
            .GreaterThan(x => x.FromDate)
            .WithErrorCode("error_todate_before_fromdate");
            
        RuleFor(x => x.PeriodType)
            .NotEmpty()
            .WithErrorCode("error_period_type_required");
    }
}
