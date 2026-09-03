using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Interfaces;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using Domain.Shared.Exceptions;
using NodaTime;

namespace Application.ExternalSync.Commands.ProcessExternalActivitySync;

/// <summary>
/// Converts normalized provider activities into idempotent ActivityLogs on eligible Plan aggregates.
/// </summary>
/// <param name="memberRepository">The repository for accessing member data.</param>
/// <param name="planRepository">The repository for accessing plan data.</param>
/// <param name="activitySyncAdapters">The collection of available activity sync adapters.</param>
/// <param name="clock">The system clock used for timestamps.</param>
public sealed class ProcessExternalActivitySyncCommandHandler(
    IMemberRepository memberRepository,
    IPlanRepository planRepository,
    IEnumerable<IActivitySyncAdapter> activitySyncAdapters,
    IClock clock)
    : ICommandHandler<ProcessExternalActivitySyncCommand, ProcessExternalActivitySyncResult>
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no activity adapter is registered for the requested provider.</exception>
    public Task<ProcessExternalActivitySyncResult> Handle(
        ProcessExternalActivitySyncCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return HandleAsync(request, cancellationToken);
    }

    private async Task<ProcessExternalActivitySyncResult> HandleAsync(
        ProcessExternalActivitySyncCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var adapter = activitySyncAdapters.FirstOrDefault(candidate =>
            candidate.ProviderId.Equals(request.ProviderId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No activity adapter is registered for '{request.ProviderId}'.");

        var member = await memberRepository.GetByActiveExternalConnectionAsync(
            request.ProviderId,
            ExternalConnectionType.ActivitiesSync.Id,
            request.ExternalUserId,
            cancellationToken);

        // Disconnect and deauthorization may race already-queued webhook work. Such work is a safe no-op.
        if (member is null)
        {
            return new ProcessExternalActivitySyncResult(0);
        }

        await adapter.MarkRunningAsync(request.ExternalUserId, request.Trigger, cancellationToken);

        var linkedPlanIds = member.PersonalPlans
            .Where(personalPlan => personalPlan.LinkUserAdapter)
            .Select(personalPlan => personalPlan.PlanId)
            .Distinct()
            .ToArray();

        var plans = await planRepository.GetByIdsAsync(linkedPlanIds, cancellationToken);
        var linkedPlanIdSet = linkedPlanIds.ToHashSet();
        var eligiblePlans = plans
            .Where(plan => linkedPlanIdSet.Contains(plan.Id) &&
                           ExternalActivityPlanPolicy.IsEligible(plan, linkUserAdapter: true))
            .ToArray();

        if (eligiblePlans.Length == 0)
        {
            await adapter.MarkSucceededAsync(
                request.ExternalUserId,
                request.Trigger,
                historicalWatermark: null,
                logsCreated: 0,
                cancellationToken);
            return new ProcessExternalActivitySyncResult(0);
        }

        var runUpperBound = clock.GetCurrentInstant();
        ActivitySyncFetchResultDto fetchResult;
        if (string.IsNullOrWhiteSpace(request.ExternalActivityId))
        {
            var rangeStart = eligiblePlans.Min(plan => plan.From);
            fetchResult = await adapter.FetchActivitiesAsync(
                request.ExternalUserId,
                rangeStart,
                runUpperBound,
                cancellationToken);
        }
        else
        {
            fetchResult = await adapter.FetchActivityAsync(
                request.ExternalUserId,
                request.ExternalActivityId,
                cancellationToken);
        }

        var nonSuccessResult = await HandleNonSuccessAsync(
            adapter,
            request,
            fetchResult,
            runUpperBound,
            cancellationToken);
        if (nonSuccessResult is not null)
        {
            return nonSuccessResult;
        }

        var (changedPlans, logsCreated) = ProcessActivities(
            fetchResult.Activities,
            eligiblePlans,
            request.ProviderId,
            runUpperBound,
            member.Id);

        foreach (var plan in changedPlans)
        {
            await planRepository.UpdateAsync(plan, cancellationToken);
        }

        if (changedPlans.Count > 0)
        {
            await planRepository.SaveChangesAsync(cancellationToken);
        }

        await adapter.MarkSucceededAsync(
            request.ExternalUserId,
            request.Trigger,
            string.IsNullOrWhiteSpace(request.ExternalActivityId)
                ? fetchResult.WatermarkCandidate
                : null,
            logsCreated,
            cancellationToken);

        return new ProcessExternalActivitySyncResult(logsCreated);
    }

    private static async Task<ProcessExternalActivitySyncResult?> HandleNonSuccessAsync(
        IActivitySyncAdapter adapter,
        ExternalActivitySyncJobRequest request,
        ActivitySyncFetchResultDto fetchResult,
        Instant now,
        CancellationToken cancellationToken)
    {
        if (fetchResult.Outcome == ActivitySyncOutcome.Success)
        {
            return null;
        }

        if (fetchResult.Outcome == ActivitySyncOutcome.NotFound)
        {
            await adapter.MarkSucceededAsync(
                request.ExternalUserId,
                request.Trigger,
                historicalWatermark: null,
                logsCreated: 0,
                cancellationToken);
            return new ProcessExternalActivitySyncResult(0);
        }

        var errorCode = fetchResult.ErrorCode;
        if (errorCode is null)
        {
            if (fetchResult.Outcome == ActivitySyncOutcome.AuthorizationRequired)
            {
                errorCode = "external_authorization_required";
            }
            else if (fetchResult.Outcome == ActivitySyncOutcome.RateLimited)
            {
                errorCode = "external_rate_limited";
            }
            else
            {
                errorCode = "external_provider_unavailable";
            }
        }

        if (fetchResult.Outcome == ActivitySyncOutcome.RateLimited || fetchResult.Outcome == ActivitySyncOutcome.TransientFailure)
        {
            var retryAt = fetchResult.RetryAt ?? now.Plus(Duration.FromMinutes(5));
            await adapter.MarkDeferredAsync(
                request.ExternalUserId,
                request.Trigger,
                retryAt,
                errorCode,
                cancellationToken);
            return new ProcessExternalActivitySyncResult(0, retryAt, errorCode);
        }

        await adapter.MarkFailedAsync(
            request.ExternalUserId,
            request.Trigger,
            errorCode,
            cancellationToken);
        return new ProcessExternalActivitySyncResult(0, ErrorCode: errorCode);
    }

    private (HashSet<Plan> ChangedPlans, int LogsCreated) ProcessActivities(
        IEnumerable<AdapterActivityDto> activities,
        IEnumerable<Plan> eligiblePlans,
        string providerId,
        Instant runUpperBound,
        Guid memberId)
    {
        var externalProvider = ExternalProvider.FromId(providerId);
        var changedPlans = new HashSet<Plan>();
        var logsCreated = 0;

        foreach (var activity in activities)
        {
            if (!activity.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            logsCreated += ProcessActivityForPlans(
                activity,
                eligiblePlans,
                externalProvider,
                runUpperBound,
                memberId,
                changedPlans);
        }

        return (changedPlans, logsCreated);
    }

    private int ProcessActivityForPlans(
        AdapterActivityDto activity,
        IEnumerable<Plan> eligiblePlans,
        ExternalProvider externalProvider,
        Instant runUpperBound,
        Guid memberId,
        HashSet<Plan> changedPlans)
    {
        var logsCreated = 0;
        foreach (var plan in eligiblePlans)
        {
            if (!ExternalActivityPlanPolicy.TryMatch(
                    plan,
                    activity.CanonicalSportTypeId,
                    activity.OccurredAt,
                    runUpperBound,
                    out var activityLocalDate))
            {
                continue;
            }

            var value = ActivityDistance.ConvertMeters(activity.DistanceMeters, plan.Unit);
            if (value is null)
            {
                continue;
            }

            try
            {
                plan.AddActivityLog(
                    value.Value,
                    activityLocalDate,
                    new ExternalActivitySource(externalProvider, activity.ExternalActivityId),
                    activity.OccurredAt,
                    clock,
                    memberId);
                changedPlans.Add(plan);
                logsCreated++;
            }
            catch (DuplicateExternalActivityException)
            {
                // A replay after a checkpoint failure or duplicate webhook is successful by definition.
            }
        }
        return logsCreated;
    }
}
