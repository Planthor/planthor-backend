using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Plans;
using NodaTime;

namespace Application.Members.ActivityLogs.Queries.List;

/// <summary>
/// Handler for listing activity logs of a specific plan with chronological cursor pagination.
/// </summary>
/// <param name="readOnlyContext">The read-only context used for querying data.</param>
public class ListActivityLogsQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<ListActivityLogsQuery, CursorPagedResult<ActivityLogDto>>
{
    /// <inheritdoc />
    public Task<CursorPagedResult<ActivityLogDto>> Handle(ListActivityLogsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readOnlyContext);
        ArgumentNullException.ThrowIfNull(request);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<CursorPagedResult<ActivityLogDto>> HandleAsync(ListActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var plan = await readOnlyContext.FirstOrDefaultAsync<Plan, Plan>(
            q => q.Where(p => p.Id == request.PlanId),
            cancellationToken);

        if (plan == null)
        {
            return new CursorPagedResult<ActivityLogDto>([], null, false);
        }

        var logs = plan.ActivityLogs.AsQueryable();

        // Sort descending: newest first. 
        logs = logs.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);

        // Apply cursor filtering
        var decodedCursor = OpaqueCursor.Decode(request.Cursor);
        if (!string.IsNullOrWhiteSpace(decodedCursor))
        {
            var parts = decodedCursor.Split('_');
            if (parts.Length == 2 && 
                long.TryParse(parts[0], out var cursorMillis) && 
                Guid.TryParse(parts[1], out var cursorId))
            {
                var cursorInstant = Instant.FromUnixTimeMilliseconds(cursorMillis);
                logs = logs.Where(x => x.CreatedAt < cursorInstant || 
                                      (x.CreatedAt == cursorInstant && x.Id.CompareTo(cursorId) < 0));
            }
        }

        var pagedLogs = logs.Take(request.Limit + 1).ToList();
        var hasNextPage = pagedLogs.Count > request.Limit;
        var paginatedLogs = pagedLogs.Take(request.Limit).ToList();

        if (paginatedLogs.Count == 0)
        {
            return new CursorPagedResult<ActivityLogDto>([], null, false);
        }

        var dtos = paginatedLogs.Select(log => new ActivityLogDto(
            log.Id,
            log.PlanId,
            log.Value,
            log.ActivityLocalDate,
            log.CompletedDate.ToDateTimeOffset(),
            log.ExternalSource?.Provider.Id,
            log.ExternalSource?.ExternalActivityId
        )).ToList();

        string? nextCursor = null;
        if (hasNextPage)
        {
            var lastLog = paginatedLogs.Last();
            var plainCursor = $"{lastLog.CreatedAt.ToUnixTimeMilliseconds()}_{lastLog.Id}";
            nextCursor = OpaqueCursor.Encode(plainCursor);
        }

        return new CursorPagedResult<ActivityLogDto>(dtos, nextCursor, hasNextPage);
    }
}
