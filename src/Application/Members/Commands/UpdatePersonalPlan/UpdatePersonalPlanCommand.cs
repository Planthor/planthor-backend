using System;
using System.Text.Json.Serialization;
using Application.Shared;

namespace Application.Members.Commands.UpdatePersonalPlan;

public record UpdatePlanCommand(
    [property: JsonIgnore] string IdentifyName,
    [property: JsonIgnore] Guid PlanId,
    string Unit,
    double Target,
    double Current,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string PeriodType)
    : ICommand;
