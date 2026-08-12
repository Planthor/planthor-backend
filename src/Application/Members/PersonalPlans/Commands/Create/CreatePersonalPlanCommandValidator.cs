using System;
using System.Linq;
using Domain.Plans;
using FluentValidation;
using NodaTime;

namespace Application.Members.PersonalPlans.Commands.Create;

/// <summary>
/// Validates the payload required to create a new plan and subscribe a member to it.
/// </summary>
public class CreatePersonalPlanCommandValidator : AbstractValidator<CreatePersonalPlanCommand>
{
    private const int MaxPlanNameLength = 100;
    private const int MaxUnitLength = 50;
    private const int MinTarget = 0;
    private const int MinPriority = 0;
    private const int MaxPriority = 999;
    private const string SportTypesPropertyName = "SportTypes";

    public CreatePersonalPlanCommandValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty().WithErrorCode("error_identity_name_required");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("error_plan_name_required")
            .MaximumLength(MaxPlanNameLength).WithErrorCode("error_plan_name_too_long");

        RuleFor(x => x.Unit)
            .NotEmpty().WithErrorCode("error_unit_required")
            .MaximumLength(MaxUnitLength).WithErrorCode("error_unit_too_long");

        RuleFor(x => x.Target)
            .GreaterThan(MinTarget).WithErrorCode("error_target_invalid");

        RuleFor(x => x.ToDate)
            .GreaterThan(x => x.FromDate)
            .WithErrorCode("error_todate_before_fromdate");

        RuleFor(x => x.StartDateLocal)
            .NotEmpty().WithErrorCode("error_start_date_local_required");

        RuleFor(x => x.EndDateLocal)
            .NotEmpty().WithErrorCode("error_end_date_local_required");

        RuleFor(x => x.EndDateLocal)
            .Must((cmd, endDate) => string.Compare(cmd.StartDateLocal, endDate, StringComparison.Ordinal) <= 0)
            .When(cmd => !string.IsNullOrEmpty(cmd.StartDateLocal) && !string.IsNullOrEmpty(cmd.EndDateLocal))
            .WithErrorCode("error_end_date_before_start_date");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithErrorCode("error_timezone_required")
            .Must(tz => !string.IsNullOrEmpty(tz) && DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz) is not null)
            .WithErrorCode("error_timezone_invalid");

        RuleFor(x => x.Prioritize)
            .InclusiveBetween(MinPriority, MaxPriority).WithErrorCode("error_priority_invalid");

        When(x => x.PlanDetails is CreateSportPlanDetailsCommand, () =>
        {
            RuleFor(x => x.PlanDetails)
                .Must(HasSportTypes)
                .WithErrorCode("error_sport_types_required")
                .OverridePropertyName(SportTypesPropertyName);

            RuleFor(x => x.PlanDetails)
                .Must(CannotCombineAll)
                .WithErrorCode("error_sport_types_cannot_combine_all")
                .OverridePropertyName(SportTypesPropertyName);

            RuleFor(x => x.PlanDetails)
                .Must(SportTypeNotEmpty)
                .WithErrorCode("error_sport_type_empty")
                .OverridePropertyName(SportTypesPropertyName);

            RuleFor(x => x.PlanDetails)
                .Must(SportTypeValid)
                .WithErrorCode("error_sport_type_invalid")
                .OverridePropertyName(SportTypesPropertyName);
        });
    }

    private static bool HasSportTypes(object? pd)
    {
        return (pd as CreateSportPlanDetailsCommand)?.SportTypes?.Count > 0;
    }

    private static bool CannotCombineAll(object? pd)
    {
        if (pd is not CreateSportPlanDetailsCommand sportDetails)
        {
            return true;
        }

        var st = sportDetails.SportTypes;
        if (st == null)
        {
            return true;
        }

        var hasAll = st.Any(s => string.Equals(s, PlanthorSportType.All.Id, StringComparison.OrdinalIgnoreCase));
        return !hasAll || st.Count == 1;
    }

    private static bool SportTypeNotEmpty(object? pd)
    {
        if (pd is not CreateSportPlanDetailsCommand sportDetails)
        {
            return true;
        }

        var st = sportDetails.SportTypes;
        if (st == null)
        {
            return true;
        }

        return st.All(s => !string.IsNullOrWhiteSpace(s));
    }

    private static bool SportTypeValid(object? pd)
    {
        if (pd is not CreateSportPlanDetailsCommand sportDetails)
        {
            return true;
        }

        var st = sportDetails.SportTypes;
        if (st == null)
        {
            return true;
        }

        return st.All(s => string.IsNullOrWhiteSpace(s) || PlanthorSportType.TryFromId(s, out _));
    }
}
