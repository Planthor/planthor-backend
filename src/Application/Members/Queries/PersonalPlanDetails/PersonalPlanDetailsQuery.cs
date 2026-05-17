using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.PersonalPlanDetails;

public sealed record PersonalPlanDetailsQuery(string IdentifyName, Guid PlanId) : IQuery<PersonalPlanDto>;
