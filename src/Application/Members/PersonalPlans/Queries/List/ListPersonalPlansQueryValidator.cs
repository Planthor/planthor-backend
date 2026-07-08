using FluentValidation;

namespace Application.Members.PersonalPlans.Queries.List;

// TODO - Trung: Revisit when verify details.
public class ListPersonalPlansQueryValidator : AbstractValidator<ListPersonalPlansQuery>
{
    private const int MinLimit = 0;
    private const int MaxLimit = 100;

    public ListPersonalPlansQueryValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty()
            .WithMessage("IdentifyName is required.");

        RuleFor(x => x.Limit)
            .GreaterThan(MinLimit)
            .WithMessage("Limit must be greater than zero.")
            .LessThanOrEqualTo(MaxLimit)
            .WithMessage($"Limit cannot exceed {MaxLimit}.");
    }
}
