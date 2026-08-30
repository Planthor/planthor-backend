using FluentValidation;

namespace Application.Members.ActivityLogs.Queries.List;

/// <summary>
/// Validator for the <see cref="ListActivityLogsQuery"/> to ensure pagination limits are valid.
/// </summary>
public sealed class ListActivityLogsQueryValidator : AbstractValidator<ListActivityLogsQuery>
{
    private const int MinLimit = 0;
    private const int MaxLimit = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListActivityLogsQueryValidator"/> class.
    /// </summary>
    public ListActivityLogsQueryValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithErrorCode("error_plan_id_required");

        RuleFor(x => x.Limit)
            .GreaterThan(MinLimit)
            .WithErrorCode("error_limit_too_low")
            .LessThanOrEqualTo(MaxLimit)
            .WithErrorCode("error_limit_too_high");
    }
}
