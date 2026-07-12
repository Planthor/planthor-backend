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

namespace Application.Members.PersonalPlans.Commands.Cancel;

/// <summary>
/// Handler for canceling a personal plan.
/// </summary>
public class CancelPlanCommandHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository,
    IClock clock)
    : ICommandHandler<CancelPlanCommand, PersonalPlanDto>
{
    private readonly IMemberRepository _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
    private readonly IPlanRepository _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    public Task<PersonalPlanDto> Handle(CancelPlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<PersonalPlanDto> HandleAsync(CancelPlanCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch member
        var member = await _memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken) 
            ?? throw new KeyNotFoundException($"Member with identifier '{request.IdentifyName}' was not found.");

        // 2. Validate member owns the plan
        var personalPlan = member.PersonalPlans.FirstOrDefault(p => p.PlanId == request.PlanId) 
            ?? throw new KeyNotFoundException($"Personal plan with PlanID '{request.PlanId}' for member '{request.IdentifyName}' was not found.");

        // 3. Fetch plan
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken) 
            ?? throw new KeyNotFoundException($"Plan with PlanID '{request.PlanId}' was not found.");

        // 4. Update domain model
        plan.Cancel(member.Id, _clock);

        // 5. Persist
        await _planRepository.UpdateAsync(plan, cancellationToken);
        await _planRepository.SaveChangesAsync(cancellationToken);

        // 6. Map to Dto
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
