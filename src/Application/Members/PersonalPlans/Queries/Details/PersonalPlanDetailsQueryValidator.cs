using FluentValidation;

namespace Application.Members.PersonalPlans.Queries.Details;

/// <summary>
/// Validator for the <see cref="PersonalPlanDetailsQuery"/> to ensure both the member identity and plan ID are provided.
/// </summary>
public sealed class PersonalPlanDetailsQueryValidator : AbstractValidator<PersonalPlanDetailsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalPlanDetailsQueryValidator"/> class.
    /// </summary>
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
