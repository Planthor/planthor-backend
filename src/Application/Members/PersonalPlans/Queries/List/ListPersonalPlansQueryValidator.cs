using FluentValidation;

namespace Application.Members.PersonalPlans.Queries.List;

// TODO - Trung: Revisit when verify details.
public class ListPersonalPlansQueryValidator : AbstractValidator<ListPersonalPlansQuery>
{
    public ListPersonalPlansQueryValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithMessage("IdentifyName is required.");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than zero.")
            .LessThanOrEqualTo(100)
            .WithMessage("Limit cannot exceed 100.");
    }
}
