using System;
using System.Collections.Generic;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.ListPersonalPlans;

public sealed record ListPersonalPlansQuery(Guid MemberId) : IQuery<IEnumerable<PersonalPlanDto>>;
