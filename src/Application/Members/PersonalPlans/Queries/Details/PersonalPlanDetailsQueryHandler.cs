using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;
using Domain.Plans;

namespace Application.Members.PersonalPlans.Queries.Details;

/// <summary>
/// Handler for retrieving details of a specific personal plan.
/// </summary>
public class PersonalPlanDetailsQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<PersonalPlanDetailsQuery, PersonalPlanDto>
{
    /// <inheritdoc />
    public async Task<PersonalPlanDto> Handle(PersonalPlanDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await readOnlyContext.FirstOrDefaultAsync<Member, Member>(
            q => q.Where(m => m.IdentifyName == request.IdentifyName),
            cancellationToken);

        var personalPlan = (member?.PersonalPlans.FirstOrDefault(p => p.PlanId == request.PlanId))
            ?? throw new KeyNotFoundException($"Personal plan with PlanID '{request.PlanId}' for member '{request.IdentifyName}' was not found.");

        var plan = await readOnlyContext.FirstOrDefaultAsync<Plan, Plan>(
            q => q.Where(p => p.Id == request.PlanId),
            cancellationToken) ?? throw new KeyNotFoundException($"Plan with PlanID '{request.PlanId}' was not found.");

        var progressPercentage = plan.Target > 0 ? Math.Round((double)plan.CurrentValue / plan.Target * 100, 2) : 0;

        return new PersonalPlanDto(
            personalPlan.PlanId,
            personalPlan.MemberId,
            personalPlan.DisplayOnProfile,
            personalPlan.Prioritize,
            personalPlan.LinkUserAdapter,
            plan.Name,
            plan.Unit,
            plan.Target,
            plan.CurrentValue,
            progressPercentage,
            plan.Status.I18NKey,
            DateTimeOffset.FromUnixTimeSeconds(plan.From.ToUnixTimeSeconds()),
            DateTimeOffset.FromUnixTimeSeconds(plan.To.ToUnixTimeSeconds())
        );
    }
}
