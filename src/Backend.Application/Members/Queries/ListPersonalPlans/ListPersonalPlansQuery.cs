using System;
using System.Collections.Generic;
using Backend.Application.Dtos;
using Backend.Application.Shared;

namespace Backend.Application.Members.Queries.ListPersonalPlans;

public sealed record ListPersonalPlansQuery(Guid MemberId) : IQuery<IEnumerable<PersonalPlanDto>>;
