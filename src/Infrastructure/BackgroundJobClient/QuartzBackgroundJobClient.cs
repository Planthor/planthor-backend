using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Microsoft.Extensions.Logging;
using Infrastructure.BackgroundJobClient.Jobs;
using Quartz;

namespace Infrastructure.BackgroundJobClient;

/// <summary>
/// Quartz implementation of the Application background-work boundary.
/// </summary>
/// <param name="schedulerFactory">The factory used to resolve the Quartz scheduler.</param>
/// <param name="logger">The logger instance.</param>
public sealed partial class QuartzBackgroundJobClient(
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzBackgroundJobClient> logger) : IBackgroundJobClient
{
    private readonly ISchedulerFactory _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
    /// <summary>The Quartz group name used for external activity synchronization jobs.</summary>
    private const string ActivityJobGroup = "external-activity-sync";

    /// <summary>The Quartz group name used for external connection revocation jobs.</summary>
    private const string RevocationJobGroup = "external-connection-revocation";

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="avatarUrl"/> is null.</exception>
    public Task EnqueueAvatarDownloadAsync(
        Guid memberId,
        Uri avatarUrl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(avatarUrl);

        return Core();

        async Task Core()
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.TriggerJob(new JobKey("DownloadAvatar"), new JobDataMap
            {
                { "MemberId", memberId.ToString() },
                { "Url", avatarUrl.ToString() }
            }, cancellationToken);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="identifyName"/> is null or empty.</exception>
    public Task EnqueueIdentitySyncAsync(
        Guid memberId,
        string identifyName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        return Core();

        async Task Core()
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.TriggerJob(new JobKey("SyncIdentity"), new JobDataMap
            {
                { "MemberId", memberId.ToString() },
                { "IdentifyName", identifyName }
            }, cancellationToken);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public Task EnqueueExternalActivitySyncAsync(
        ExternalActivitySyncJobRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task Core()
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKey = GetActivityJobKey(request.ProviderId, request.ExternalUserId);
            await EnsureActivityJobAsync(scheduler, jobKey, request, cancellationToken);

            var triggerBuilder = TriggerBuilder.Create()
                .WithIdentity(GetTriggerKey(request.IdempotencyKey, ActivityJobGroup))
                .ForJob(jobKey)
                .UsingJobData("Trigger", request.Trigger)
                .UsingJobData("IdempotencyKey", request.IdempotencyKey)
                .UsingJobData("ExternalActivityId", request.ExternalActivityId ?? string.Empty)
                .UsingJobData("RetryCount", request.RetryCount.ToString(CultureInfo.InvariantCulture));

            triggerBuilder = request.NotBefore is null
                ? triggerBuilder.StartNow()
                : triggerBuilder.StartAt(request.NotBefore.Value.ToDateTimeOffset());

            var trigger = triggerBuilder
                .WithSimpleSchedule(schedule => schedule.WithMisfireHandlingInstructionFireNow())
                .Build();

            try
            {
                await scheduler.ScheduleJob(trigger, cancellationToken);
            }
            catch (ObjectAlreadyExistsException)
            {
                LogCoalescedActivitySyncTrigger(trigger.Key);
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="providerId"/>, <paramref name="externalUserId"/>, or <paramref name="idempotencyKey"/> is null or empty.</exception>
    public Task EnqueueExternalConnectionRevocationAsync(
        string providerId,
        string externalUserId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);
        ArgumentException.ThrowIfNullOrEmpty(idempotencyKey);

        return Core();

        async Task Core()
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKey = new JobKey(
                $"{Normalize(providerId)}-{Hash(externalUserId)}",
                RevocationJobGroup);
            var job = JobBuilder.Create<RevokeExternalConnectionJob>()
                .WithIdentity(jobKey)
                .UsingJobData("ProviderId", providerId)
                .UsingJobData("ExternalUserId", externalUserId)
                .StoreDurably()
                .RequestRecovery()
                .Build();

            try
            {
                await scheduler.AddJob(job, replace: false, storeNonDurableWhileAwaitingScheduling: false, cancellationToken);
            }
            catch (ObjectAlreadyExistsException)
            {
                LogCoalescedRevocationJob(jobKey);
            }

            var trigger = TriggerBuilder.Create()
                .WithIdentity(GetTriggerKey(idempotencyKey, RevocationJobGroup))
                .ForJob(jobKey)
                .StartNow()
                .WithSimpleSchedule(schedule => schedule.WithMisfireHandlingInstructionFireNow())
                .Build();

            try
            {
                await scheduler.ScheduleJob(trigger, cancellationToken);
            }
            catch (ObjectAlreadyExistsException)
            {
                LogCoalescedRevocationTrigger(trigger.Key);
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="providerId"/> or <paramref name="externalUserId"/> is null or empty.</exception>
    public Task CancelExternalActivitySyncAsync(
        string providerId,
        string externalUserId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);

        return Core();

        async Task Core()
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.DeleteJob(GetActivityJobKey(providerId, externalUserId), cancellationToken);
        }
    }

    /// <summary>
    /// Ensures that a durable job for synchronizing activities exists for the specified athlete.
    /// Does nothing if the job has already been materialized by this or another cluster node.
    /// </summary>
    /// <param name="scheduler">The Quartz scheduler.</param>
    /// <param name="jobKey">The unique job key for the athlete.</param>
    /// <param name="request">The incoming activity sync request.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    private async Task EnsureActivityJobAsync(
        IScheduler scheduler,
        JobKey jobKey,
        ExternalActivitySyncJobRequest request,
        CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<ProcessExternalActivitySyncJob>()
            .WithIdentity(jobKey)
            .UsingJobData("ProviderId", request.ProviderId)
            .UsingJobData("ExternalUserId", request.ExternalUserId)
            .StoreDurably()
            .RequestRecovery()
            .Build();

        try
        {
            await scheduler.AddJob(job, replace: false, storeNonDurableWhileAwaitingScheduling: false, cancellationToken);
        }
        catch (ObjectAlreadyExistsException)
        {
            LogCoalescedActivitySyncJob(jobKey);
        }
    }

    /// <summary>
    /// Computes a stable Quartz job key for an athlete based on their provider and external identifier.
    /// </summary>
    /// <param name="providerId">The external provider identifier.</param>
    /// <param name="externalUserId">The athlete identifier assigned by the provider.</param>
    /// <returns>A unique <see cref="JobKey"/>.</returns>
    private static JobKey GetActivityJobKey(string providerId, string externalUserId) =>
        new($"{Normalize(providerId)}-{Hash(externalUserId)}", ActivityJobGroup);

    /// <summary>
    /// Computes a stable Quartz trigger key based on an idempotency key and group name.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key, often derived from a webhook or manual request.</param>
    /// <param name="group">The Quartz group name.</param>
    /// <returns>A unique <see cref="TriggerKey"/>.</returns>
    private static TriggerKey GetTriggerKey(string idempotencyKey, string group) =>
        new(Hash(idempotencyKey), group);

    /// <summary>
    /// Normalizes a string value by trimming whitespace and converting it to uppercase.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>The normalized string.</returns>
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    /// <summary>
    /// Computes a SHA-256 hash of the provided string and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The hashed string representation.</returns>
    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Coalesced duplicate external activity sync trigger '{TriggerKey}'")]
    private partial void LogCoalescedActivitySyncTrigger(TriggerKey triggerKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Job '{JobKey}' already exists. Coalescing durable revocation job.")]
    private partial void LogCoalescedRevocationJob(JobKey jobKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Coalesced duplicate external connection revocation trigger '{TriggerKey}'")]
    private partial void LogCoalescedRevocationTrigger(TriggerKey triggerKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Job '{JobKey}' already exists. Coalescing durable activity sync job.")]
    private partial void LogCoalescedActivitySyncJob(JobKey jobKey);
}
