using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;
using Domain.Plans;

namespace Application.Members.PersonalPlans.Queries.List;

/// <summary>
/// Handler for listing personal plans of a member with progress and cursor pagination.
/// </summary>
public class ListPersonalPlansQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<ListPersonalPlansQuery, CursorPagedResult<PersonalPlanDto>>
{
    private const double PercentageMultiplier = 100.0;
    private const int RoundingDecimals = 2;

    /// <inheritdoc />
    public Task<CursorPagedResult<PersonalPlanDto>> Handle(ListPersonalPlansQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readOnlyContext);
        ArgumentNullException.ThrowIfNull(request);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<CursorPagedResult<PersonalPlanDto>> HandleAsync(ListPersonalPlansQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch the Member's PersonalPlans
        var member = await readOnlyContext.FirstOrDefaultAsync<Member, Member>(
            q => q.Where(m => m.IdentifyName == request.IdentifyName),
            cancellationToken);

        if (member == null)
        {
            return new CursorPagedResult<PersonalPlanDto>([], null, false);
        }

        var personalPlans = member.PersonalPlans.AsQueryable();

        // 2. Apply cursor pagination on the PersonalPlans first to limit the Plans we need to fetch
        personalPlans = personalPlans.OrderBy(x => x.PlanId);

        if (request.Cursor.HasValue)
        {
            personalPlans = personalPlans.Where(x => x.PlanId.CompareTo(request.Cursor.Value) > 0);
        }

        var pagedPersonalPlans = personalPlans.Take(request.Limit + 1).ToList();
        var hasNextPage = pagedPersonalPlans.Count > request.Limit;
        var paginatedPersonalPlans = pagedPersonalPlans.Take(request.Limit).ToList();

        if (paginatedPersonalPlans.Count == 0)
        {
            return new CursorPagedResult<PersonalPlanDto>([], null, false);
        }

        // 3. Fetch the corresponding Plans from the database
        var planIds = paginatedPersonalPlans.Select(p => p.PlanId).ToList();
        var plans = await readOnlyContext.QueryAsync<Plan, Plan>(
            q => q.Where(p => planIds.Contains(p.Id)),
            cancellationToken);

        // Filter valid plans if status filter is applied
        if (request.Statuses is { Length: > 0 })
        {
            var validStatusIds = request.Statuses
                .Select(requestedStatus => PlanStatus.All.FirstOrDefault(ps => 
                    ps.Id.Equals(requestedStatus, StringComparison.OrdinalIgnoreCase) || 
                    ps.Name.Equals(requestedStatus, StringComparison.OrdinalIgnoreCase))?.Id)
                .Where(id => id != null)
                .ToList();

            if (validStatusIds.Count != 0)
            {
                plans = [.. plans.Where(p => validStatusIds.Contains(p.Status.Id))];
            }
        }

        // 4. Map to DTOs by matching PersonalPlan with Plan
        var dtos = new List<PersonalPlanDto>();
        foreach (var pp in paginatedPersonalPlans)
        {
            var plan = plans.FirstOrDefault(p => p.Id == pp.PlanId);
            if (plan == null)
            {
                continue; // Skip if filtered out by status
            }

            dtos.Add(new PersonalPlanDto(
                pp.PlanId,
                pp.MemberId,
                pp.DisplayOnProfile,
                pp.Prioritize,
                pp.LinkUserAdapter,
                plan.Name,
                plan.Unit,
                plan.Target,
                plan.CurrentValue,
                plan.Target > 0 ? Math.Round((plan.CurrentValue / plan.Target) * PercentageMultiplier, RoundingDecimals) : 0.0,
                plan.Status.Name,
                plan.From.ToDateTimeOffset(),
                plan.To.ToDateTimeOffset()
            ));
        }

        string? nextCursor = hasNextPage
            ? paginatedPersonalPlans.Last().PlanId.ToString()
            : null;

        return new CursorPagedResult<PersonalPlanDto>(dtos, nextCursor, hasNextPage);
    }
}
