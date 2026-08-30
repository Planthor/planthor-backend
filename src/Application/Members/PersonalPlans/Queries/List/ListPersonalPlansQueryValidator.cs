using FluentValidation;

namespace Application.Members.PersonalPlans.Queries.List;

/// <summary>
/// Validator for the <see cref="ListPersonalPlansQuery"/> to ensure pagination limits and member identity are valid.
/// </summary>
public sealed class ListPersonalPlansQueryValidator : AbstractValidator<ListPersonalPlansQuery>
{
    private const int MinLimit = 0;
    private const int MaxLimit = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPersonalPlansQueryValidator"/> class.
    /// </summary>
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
