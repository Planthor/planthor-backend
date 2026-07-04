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
public class CreatePersonalPlanCommandHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository,
    IClock clock) : ICommandHandler<CreatePersonalPlanCommand, Guid>
{
    public Task<Guid> Handle(CreatePersonalPlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(memberRepository);
        ArgumentNullException.ThrowIfNull(planRepository);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<Guid> HandleAsync(CreatePersonalPlanCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken)
            ?? throw new InvalidOperationException($"Member with IdentityName '{request.IdentifyName}' was not found.");

        var fromInstant = Instant.FromDateTimeOffset(request.FromDate);
        var toInstant = Instant.FromDateTimeOffset(request.ToDate);

        var plan = Plan.Create(
            request.Name,
            request.Unit,
            (float)request.Target,
            fromInstant,
            toInstant,
            request.StartDateLocal,
            request.EndDateLocal,
            request.Timezone,
            request.EnableActivityLog,
            clock,
            member.Id);

        member.SubscribeToPlan(
            plan.Id,
            request.DisplayOnProfile,
            request.Prioritize,
            request.LinkUserAdapter,
            clock);

        await planRepository.AddAsync(plan, cancellationToken);

        await planRepository.SaveChangesAsync(cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
