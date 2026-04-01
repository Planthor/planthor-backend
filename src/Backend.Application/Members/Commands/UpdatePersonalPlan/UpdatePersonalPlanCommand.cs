using System;
using System.Text.Json.Serialization;
using Backend.Application.Shared;

namespace Backend.Application.Members.Commands.UpdatePersonalPlan;

public record UpdatePlanCommand(
    [property: JsonIgnore] Guid MemberId,
    [property: JsonIgnore] Guid PlanId,
    string Unit,
    double Target,
    double Current,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string PeriodType)
    : ICommand;
