using FluentValidation;

namespace Application.Members.PersonalPlans.Commands.Update;

// TODO - Trung: Revisit when verify details.
public class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    private const int MinTarget = 0;
    private const int MinCurrent = 0;

    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithMessage("IdentifyName is required.");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithMessage("PlanId is required.");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithMessage("Unit is required.");

        RuleFor(x => x.Target)
            .GreaterThan(MinTarget)
            .WithMessage("Target must be greater than zero.");

        RuleFor(x => x.Current)
            .GreaterThanOrEqualTo(MinCurrent)
            .WithMessage("Current value cannot be negative.");

        RuleFor(x => x.ToDate)
            .GreaterThan(x => x.FromDate)
            .WithMessage("ToDate must be strictly after FromDate.");
            
        RuleFor(x => x.PeriodType)
            .NotEmpty()
            .WithMessage("PeriodType is required.");
    }
}
