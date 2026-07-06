using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Plans;

namespace Application.Members.ActivityLogs.Queries.Details;

/// <summary>
/// Handles the <see cref="ActivityLogDetailsQuery"/> to retrieve activity log details.
/// </summary>
/// <param name="planRepository">The plan repository to fetch plan details.</param>
public class ActivityLogDetailsQueryHandler(IPlanRepository planRepository) : IQueryHandler<ActivityLogDetailsQuery, ActivityLogDto>
{
    /// <summary>
    /// Handles the query to retrieve the activity log details.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the <see cref="ActivityLogDto"/>.</returns>
    public async Task<ActivityLogDto> Handle(ActivityLogDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(planRepository);

        var plan = await planRepository.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Plan with ID {request.PlanId} was not found.");

        var log = plan.ActivityLogs.SingleOrDefault(x => x.Id == request.LogId)
            ?? throw new InvalidOperationException($"Activity log with ID {request.LogId} was not found.");

        return new ActivityLogDto(
            Id: log.Id,
            PlanId: log.PlanId,
            Value: log.Value,
            ActivityLocalDate: log.ActivityLocalDate,
            CompletedDate: log.CompletedDate.ToDateTimeOffset(),
            ExternalSourceProvider: log.ExternalSource?.Provider.Id,
            ExternalSourceId: log.ExternalSource?.ExternalActivityId
        );
    }
}
