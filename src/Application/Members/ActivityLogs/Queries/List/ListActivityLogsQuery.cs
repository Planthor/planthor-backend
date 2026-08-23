using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.ActivityLogs.Queries.List;

/// <summary>
/// Query to list activity logs for a specific plan.
/// </summary>
/// <param name="PlanId">The ID of the plan.</param>
/// <param name="Limit">The maximum number of items to return.</param>
/// <param name="Cursor">The cursor for pagination (format: CreatedAtMillis_Id).</param>
public sealed record ListActivityLogsQuery(
    Guid PlanId,
    int Limit = 10,
    string? Cursor = null
) : IQuery<CursorPagedResult<ActivityLogDto>>;
