using FluentValidation;

namespace Application.Members.ActivityLogs.Queries.Details;

/// <summary>
/// Validator for the <see cref="ActivityLogDetailsQuery"/> command.
/// </summary>
public class ActivityLogDetailsQueryValidator : AbstractValidator<ActivityLogDetailsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogDetailsQueryValidator"/> class.
    /// </summary>
    public ActivityLogDetailsQueryValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.LogId).NotEmpty();
    }
}
