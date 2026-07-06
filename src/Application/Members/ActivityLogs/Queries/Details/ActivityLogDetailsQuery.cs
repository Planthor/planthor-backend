using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.ActivityLogs.Queries.Details;

/// <summary>
/// Query to retrieve the details of a specific activity log.
/// </summary>
/// <param name="PlanId">The unique identifier of the plan.</param>
/// <param name="LogId">The unique identifier of the activity log.</param>
public record ActivityLogDetailsQuery(Guid PlanId, Guid LogId) : IQuery<ActivityLogDto>;
