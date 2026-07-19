using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.PersonalPlans.Commands.Update;

/// <summary>
/// Command to update the details of a member's personal plan, such as its target, dates, and period type.
/// </summary>
public record UpdatePersonalPlanCommand(
    string IdentifyName,
    Guid PlanId,
    string Unit,
    double Target,
    double Current,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string PeriodType)
    : ICommand<PersonalPlanDto>;
