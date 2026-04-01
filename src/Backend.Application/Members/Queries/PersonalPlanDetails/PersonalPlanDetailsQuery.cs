using System;
using Backend.Application.Dtos;
using Backend.Application.Shared;

namespace Backend.Application.Members.Queries.PersonalPlanDetails;

public sealed record PersonalPlanDetailsQuery(Guid MemberId, Guid PlanId) : IQuery<PersonalPlanDto>;
