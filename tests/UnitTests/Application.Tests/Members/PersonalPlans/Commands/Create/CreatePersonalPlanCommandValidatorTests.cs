using System;
using Application.Members.PersonalPlans.Commands.Create;
using FluentValidation.TestHelper;

namespace Application.Tests.Members.PersonalPlans.Commands.Create;

public class CreatePersonalPlanCommandValidatorTests
{
    private readonly CreatePersonalPlanCommandValidator _validator = new();

    private static CreatePersonalPlanCommand CreateValidCommand() => new(
        "user1", "Plan", "km", 10f, 
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero),
        "2026-01-01", "2026-12-31", "UTC", true, true, 0, false, null);

    [Fact]
    public void Should_Have_Error_When_IdentifyName_Is_Empty()
    {
        var model = CreateValidCommand() with { IdentifyName = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.IdentifyName).WithErrorCode("error_identity_name_required");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var model = CreateValidCommand() with { Name = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorCode("error_plan_name_required");
    }

    [Fact]
    public void Should_Have_Error_When_Unit_Is_Empty()
    {
        var model = CreateValidCommand() with { Unit = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Unit).WithErrorCode("error_unit_required");
    }

    [Fact]
    public void Should_Have_Error_When_Target_Is_Zero_Or_Less()
    {
        var model = CreateValidCommand() with { Target = 0 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Target).WithErrorCode("error_target_invalid");
    }

    [Fact]
    public void Should_Have_Error_When_ToDate_Is_Before_FromDate()
    {
        var model = CreateValidCommand() with { ToDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ToDate).WithErrorCode("error_todate_before_fromdate");
    }

    [Fact]
    public void Should_Have_Error_When_StartDateLocal_Is_Empty()
    {
        var model = CreateValidCommand() with { StartDateLocal = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.StartDateLocal).WithErrorCode("error_start_date_local_required");
    }

    [Fact]
    public void Should_Have_Error_When_EndDateLocal_Is_Empty()
    {
        var model = CreateValidCommand() with { EndDateLocal = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.EndDateLocal).WithErrorCode("error_end_date_local_required");
    }

    [Fact]
    public void Should_Have_Error_When_EndDateLocal_Before_StartDateLocal()
    {
        var model = CreateValidCommand() with { StartDateLocal = "2026-12-31", EndDateLocal = "2026-01-01" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.EndDateLocal).WithErrorCode("error_end_date_before_start_date");
    }

    [Fact]
    public void Should_Have_Error_When_Timezone_Is_Empty()
    {
        var model = CreateValidCommand() with { Timezone = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Timezone).WithErrorCode("error_timezone_required");
    }

    [Fact]
    public void Should_Have_Error_When_Timezone_Is_Invalid()
    {
        var model = CreateValidCommand() with { Timezone = "Invalid/Zone" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Timezone).WithErrorCode("error_timezone_invalid");
    }

    [Fact]
    public void Should_Have_Error_When_Prioritize_Is_Negative()
    {
        var model = CreateValidCommand() with { Prioritize = -1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Prioritize).WithErrorCode("error_priority_invalid");
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Model()
    {
        var model = CreateValidCommand();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_SportTypes_Is_Empty()
    {
        var model = CreateValidCommand() with { PlanDetails = new CreateSportPlanDetailsCommand([]) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("SportTypes").WithErrorCode("error_sport_types_required");
    }

    [Fact]
    public void Should_Have_Error_When_SportTypes_Contains_All_And_Others()
    {
        var model = CreateValidCommand() with { PlanDetails = new CreateSportPlanDetailsCommand(["all", "run"]) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("SportTypes").WithErrorCode("error_sport_types_cannot_combine_all");
    }

    [Fact]
    public void Should_Have_Error_When_SportType_Is_Empty()
    {
        var model = CreateValidCommand() with { PlanDetails = new CreateSportPlanDetailsCommand([""]) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("SportTypes").WithErrorCode("error_sport_type_empty");
    }

    [Fact]
    public void Should_Have_Error_When_SportType_Is_Invalid()
    {
        var model = CreateValidCommand() with { PlanDetails = new CreateSportPlanDetailsCommand(["invalid_sport"]) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("SportTypes").WithErrorCode("error_sport_type_invalid");
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_SportPlan()
    {
        var model = CreateValidCommand() with { PlanDetails = new CreateSportPlanDetailsCommand(["run"]) };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_SportTypes_Is_Null()
    {
        var model = CreateValidCommand() with { PlanDetails = new CreateSportPlanDetailsCommand(null!) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("SportTypes").WithErrorCode("error_sport_types_required");
    }
}
