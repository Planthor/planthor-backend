using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.PersonalPlans.Queries.Details;

/// <summary>
/// Query to retrieve the detailed information of a specific personal plan for a given member.
/// </summary>
public sealed record PersonalPlanDetailsQuery(string IdentifyName, Guid PlanId) : IQuery<PersonalPlanDto>;
