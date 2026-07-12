using System;
using System.Text.Json.Serialization;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.PersonalPlans.Commands.Activate;

/// <summary>
/// Command to activate a personal plan.
/// </summary>
public record ActivatePersonalPlanCommand(
    [property: JsonIgnore] string IdentifyName,
    [property: JsonIgnore] Guid PlanId)
    : ICommand<PersonalPlanDto>;
