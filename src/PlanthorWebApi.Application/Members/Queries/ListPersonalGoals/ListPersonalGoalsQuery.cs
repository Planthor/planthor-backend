using System;
using System.Collections.Generic;
using PlanthorWebApi.Application.Dtos;
using PlanthorWebApi.Application.Shared;

namespace PlanthorWebApi.Application.Members.Queries.ListPersonalGoals;

public sealed record ListPersonalGoalsQuery(Guid MemberId) : IQuery<IEnumerable<PersonalPlanDto>>;
