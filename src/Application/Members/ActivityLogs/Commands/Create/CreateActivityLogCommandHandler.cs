using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using NodaTime;

namespace Application.Members.ActivityLogs.Commands.Create;

/// <summary>
/// Handles the creation of a new activity log entry for a specific plan.
/// Ensures the Plan's current value is recalculated according to DDD principles.
/// </summary>
public sealed class CreateActivityLogCommandHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository,
    IClock clock) : ICommandHandler<CreateActivityLogCommand, Guid>
{
    public Task<Guid> Handle(CreateActivityLogCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(memberRepository);
        ArgumentNullException.ThrowIfNull(planRepository);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<Guid> HandleAsync(CreateActivityLogCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken)
            ?? throw new InvalidOperationException($"Member with IdentityName '{request.IdentifyName}' was not found.");

        var plan = await planRepository.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Plan with ID '{request.PlanId}' was not found.");

        // Add the activity log through the Aggregate Root to protect invariants.
        // This will intrinsically trigger the RecalculateCurrentValue() inside the Plan.
        var activityLog = plan.AddActivityLog(
            request.Value,
            request.ActivityLocalDate,
            request.ExternalSource,
            clock,
            member.Id);

        await planRepository.UpdateAsync(plan, cancellationToken);
        await planRepository.SaveChangesAsync(cancellationToken);

        return activityLog.Id;
    }
}
