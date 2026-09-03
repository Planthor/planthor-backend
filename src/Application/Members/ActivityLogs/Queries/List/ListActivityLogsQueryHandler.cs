using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using NodaTime;

namespace Application.Members.ActivityLogs.Queries.List;

/// <summary>
/// Handler for listing activity logs of a specific plan with chronological cursor pagination.
/// </summary>
/// <param name="memberRepository">The member repository used to enforce Plan ownership.</param>
/// <param name="planRepository">The Plan aggregate repository.</param>
public sealed class ListActivityLogsQueryHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository)
    : IQueryHandler<ListActivityLogsQuery, CursorPagedResult<ActivityLogDto>>
{
    /// <inheritdoc />
    public Task<CursorPagedResult<ActivityLogDto>> Handle(ListActivityLogsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(memberRepository);
        ArgumentNullException.ThrowIfNull(planRepository);
        ArgumentNullException.ThrowIfNull(request);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<CursorPagedResult<ActivityLogDto>> HandleAsync(ListActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken)
            ?? throw new KeyNotFoundException("Activity ledger was not found.");
            
        if (!member.PersonalPlans.Any(personalPlan => personalPlan.PlanId == request.PlanId))
        {
            throw new KeyNotFoundException("Activity ledger was not found.");
        }

        var plan = await planRepository.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Activity ledger was not found.");

        var logs = plan.ActivityLogs.AsQueryable();

        // Sort descending: newest first. 
        logs = logs.OrderByDescending(x => x.CompletedDate).ThenByDescending(x => x.Id);

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
                logs = logs.Where(x => x.CompletedDate < cursorInstant ||
                                      (x.CompletedDate == cursorInstant && x.Id.CompareTo(cursorId) < 0));
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
            var lastLog = paginatedLogs[^1];
            var plainCursor = $"{lastLog.CompletedDate.ToUnixTimeMilliseconds()}_{lastLog.Id}";
            nextCursor = OpaqueCursor.Encode(plainCursor);
        }

        return new CursorPagedResult<ActivityLogDto>(dtos, nextCursor, hasNextPage);
    }
}
