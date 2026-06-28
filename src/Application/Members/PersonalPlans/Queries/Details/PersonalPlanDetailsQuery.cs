using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.PersonalPlans.Queries.Details;

public sealed record PersonalPlanDetailsQuery(string IdentifyName, Guid PlanId) : IQuery<PersonalPlanDto>;
