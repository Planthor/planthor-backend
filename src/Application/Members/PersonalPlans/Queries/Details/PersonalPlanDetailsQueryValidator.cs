using FluentValidation;

namespace Application.Members.PersonalPlans.Queries.Details;

// TODO - Trung: Revisit when verify details
/// <summary>
/// Validator for the <see cref="PersonalPlanDetailsQuery"/> to ensure both the member identity and plan ID are provided.
/// </summary>
public class PersonalPlanDetailsQueryValidator : AbstractValidator<PersonalPlanDetailsQuery>
{
    public PersonalPlanDetailsQueryValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithErrorCode("error_identity_name_required");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithErrorCode("error_plan_id_required");
    }
}
