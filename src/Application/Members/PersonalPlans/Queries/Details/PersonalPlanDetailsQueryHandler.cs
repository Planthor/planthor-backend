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
public sealed class PersonalPlanDetailsQueryHandler : IQueryHandler<PersonalPlanDetailsQuery, PersonalPlanDto>
{
    private readonly IReadOnlyContext _readOnlyContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalPlanDetailsQueryHandler"/> class.
    /// </summary>
    public PersonalPlanDetailsQueryHandler(IReadOnlyContext readOnlyContext)
    {
        ArgumentNullException.ThrowIfNull(readOnlyContext);
        _readOnlyContext = readOnlyContext;
    }

    private const double PercentageMultiplier = 100;
    private const int RoundingDecimals = 2;

    /// <inheritdoc />
    public Task<PersonalPlanDto> Handle(PersonalPlanDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<PersonalPlanDto> Core()
        {
            var member = await _readOnlyContext.FirstOrDefaultAsync<Member, Member>(
                q => q.Where(m => m.IdentifyName == request.IdentifyName),
                cancellationToken);

            var personalPlan = (member?.PersonalPlans.FirstOrDefault(p => p.PlanId == request.PlanId))
                ?? throw new KeyNotFoundException($"Personal plan with PlanID '{request.PlanId}' for member '{request.IdentifyName}' was not found.");

            var plan = await _readOnlyContext.FirstOrDefaultAsync(
                (IQueryable<Plan> q) => q
                    .Where(p => p.Id == request.PlanId)
                    .Select(p => new
                    {
                        p.Name,
                        p.Unit,
                        p.Target,
                        p.CurrentValue,
                        p.Status,
                        p.From,
                        p.To,
                    }),
                cancellationToken) ?? throw new KeyNotFoundException($"Plan with PlanID '{request.PlanId}' was not found.");

            var progressPercentage = Math.Round((double)plan.CurrentValue / plan.Target * PercentageMultiplier, RoundingDecimals);

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
}
