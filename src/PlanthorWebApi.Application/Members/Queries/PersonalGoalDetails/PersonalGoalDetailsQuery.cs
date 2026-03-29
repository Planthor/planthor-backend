using System;
using PlanthorWebApi.Application.Dtos;
using PlanthorWebApi.Application.Shared;

namespace PlanthorWebApi.Application.Members.Queries.PersonalGoalDetails;

public sealed record PersonalGoalDetailsQuery(Guid MemberId, Guid GoalId) : IQuery<PersonalPlanDto>;
