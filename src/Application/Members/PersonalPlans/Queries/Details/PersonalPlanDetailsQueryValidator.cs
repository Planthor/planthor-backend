using FluentValidation;

namespace Application.Members.PersonalPlans.Queries.Details;

// TODO - Trung: Revisit when verify details
public class PersonalPlanDetailsQueryValidator : AbstractValidator<PersonalPlanDetailsQuery>
{
    public PersonalPlanDetailsQueryValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithMessage("IdentifyName is required.");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithMessage("PlanId is required.");
    }
}
