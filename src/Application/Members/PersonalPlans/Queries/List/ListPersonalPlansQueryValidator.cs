using FluentValidation;

namespace Application.Members.PersonalPlans.Queries.List;

// TODO - Trung: Revisit when verify details.
/// <summary>
/// Validator for the <see cref="ListPersonalPlansQuery"/> to ensure pagination limits and member identity are valid.
/// </summary>
public class ListPersonalPlansQueryValidator : AbstractValidator<ListPersonalPlansQuery>
{
    private const int MinLimit = 0;
    private const int MaxLimit = 100;

    public ListPersonalPlansQueryValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithErrorCode("error_identity_name_required");

        RuleFor(x => x.Limit)
            .GreaterThan(MinLimit)
            .WithErrorCode("error_limit_too_low")
            .LessThanOrEqualTo(MaxLimit)
            .WithErrorCode("error_limit_too_high");
    }
}
