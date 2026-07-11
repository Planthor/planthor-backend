using System;
using System.Text.Json.Serialization;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.PersonalPlans.Commands.Cancel;

/// <summary>
/// Command to cancel a personal plan.
/// </summary>
public record CancelPlanCommand(
    [property: JsonIgnore] string IdentifyName,
    [property: JsonIgnore] Guid PlanId)
    : ICommand<PersonalPlanDto>;
