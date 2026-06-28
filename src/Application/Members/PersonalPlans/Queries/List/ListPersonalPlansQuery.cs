using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.PersonalPlans.Queries.List;

/// <summary>
/// 
/// </summary>
/// <param name="IdentifyName"></param>
/// <param name="Limit"></param>
/// <param name="Cursor"></param>
/// <param name="Statuses"></param>
public sealed record ListPersonalPlansQuery(
    string IdentifyName,
    int Limit = 10,
    Guid? Cursor = null,
    string[]? Statuses = null
) : IQuery<CursorPagedResult<PersonalPlanDto>>;
