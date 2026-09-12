using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using NodaTime;

namespace Application.Members.PersonalPlans.Commands.Create;

/// <summary>
/// Handles the creation of a new plan and subscribing a member to it as a personal plan.
/// </summary>
/// <param name="memberRepository">The repository used to access and modify member data.</param>
/// <param name="planRepository">The repository used to access and modify plan data.</param>
/// <param name="clock">The clock used to get the current time for auditing updates.</param>
public sealed class CreatePersonalPlanCommandHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository,
    IClock clock) : ICommandHandler<CreatePersonalPlanCommand, Guid>
{
    private readonly IMemberRepository _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
    private readonly IPlanRepository _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    public Task<Guid> Handle(CreatePersonalPlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<Guid> HandleAsync(CreatePersonalPlanCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken)
            ?? throw new InvalidOperationException($"Member with IdentityName '{request.IdentifyName}' was not found.");

        var fromInstant = Instant.FromDateTimeOffset(request.FromDate);
        var toInstant = Instant.FromDateTimeOffset(request.ToDate);
        var linkUserAdapter = request.LinkUserAdapter ?? member.ResolveLinkUserAdapterForNewPlan();
        var enableActivityLog = request.EnableActivityLog || linkUserAdapter;

        var plan = request.PlanDetails is CreateSportPlanDetailsCommand sportDetails 
            ? Plan.CreateSportPlan(
                request.Name,
                request.Unit,
                (float)request.Target,
                fromInstant,
                toInstant,
                request.StartDateLocal,
                request.EndDateLocal,
                request.Timezone,
                enableActivityLog,
                new SportPlanDetails(request.Unit, [.. sportDetails.SportTypes]),
                _clock,
                member.Id)
            : Plan.Create(
                request.Name,
                request.Unit,
                (float)request.Target,
                fromInstant,
                toInstant,
                request.StartDateLocal,
                request.EndDateLocal,
                request.Timezone,
                enableActivityLog,
                _clock,
                member.Id);

        member.SubscribeToPlan(
            plan.Id,
            request.DisplayOnProfile,
            request.Prioritize,
            linkUserAdapter,
            _clock);

        await _planRepository.AddAsync(plan, cancellationToken);
        await _planRepository.SaveChangesAsync(cancellationToken); // Already cover Member, Plan, Personal Plan

        return plan.Id;
    }
}
