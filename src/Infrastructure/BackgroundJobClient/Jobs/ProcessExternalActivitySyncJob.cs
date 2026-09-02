using System;
using System.Globalization;
using System.Threading.Tasks;
using Application.ExternalSync.Commands.ProcessExternalActivitySync;
using Application.Shared;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;

namespace Infrastructure.BackgroundJobClient.Jobs;

/// <summary>
/// Executes provider-neutral activity synchronization for one serialized provider athlete.
/// </summary>
/// <remarks>
/// Marked with <see cref="DisallowConcurrentExecutionAttribute"/> to serialize syncing per athlete,
/// preventing database concurrency exceptions and protecting external API rate limits.
/// </remarks>
/// <param name="sender">The MediatR sender used to dispatch the synchronization command.</param>
/// <param name="backgroundJobClient">The background job client used to schedule retries.</param>
/// <param name="clock">The system clock used to compute retry delays.</param>
/// <param name="logger">The logger instance.</param>
[DisallowConcurrentExecution]
public sealed partial class ProcessExternalActivitySyncJob(
    ISender sender,
    IBackgroundJobClient backgroundJobClient,
    IClock clock,
    ILogger<ProcessExternalActivitySyncJob> logger) : IJob
{
    private const int MaximumInfrastructureRetries = 3;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <c>ProviderId</c> or <c>ExternalUserId</c> is missing from the job data.</exception>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = context.MergedJobDataMap;
        var providerId = data.GetString("ProviderId") ?? throw new InvalidOperationException("ProviderId is missing.");
        var externalUserId = data.GetString("ExternalUserId") ?? throw new InvalidOperationException("ExternalUserId is missing.");
        var trigger = data.GetString("Trigger") ?? ExternalActivitySyncTrigger.Retry;
        var idempotencyKey = data.GetString("IdempotencyKey") ?? Guid.NewGuid().ToString("N");
        var activityId = data.GetString("ExternalActivityId");
        _ = int.TryParse(data.GetString("RetryCount"), CultureInfo.InvariantCulture, out var retryCount);

        var request = new ExternalActivitySyncJobRequest(
            providerId,
            externalUserId,
            trigger,
            idempotencyKey,
            string.IsNullOrWhiteSpace(activityId) ? null : activityId,
            RetryCount: retryCount);

        try
        {
            var result = await sender.Send(
                new ProcessExternalActivitySyncCommand(request),
                context.CancellationToken);

            if (result.RetryAt is not null)
            {
                await EnqueueRetryAsync(request, result.RetryAt.Value, retryCount, context);
            }
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (retryCount < MaximumInfrastructureRetries)
        {
            LogRetry(exception, providerId, externalUserId, retryCount + 1);
            var delays = new[] { 1, 5, 15 };
            var retryAt = clock.GetCurrentInstant().Plus(Duration.FromMinutes(delays[retryCount]));
            await EnqueueRetryAsync(request, retryAt, retryCount + 1, context);
        }
    }

    /// <summary>
    /// Enqueues a delayed retry of the activity sync job when the provider is rate-limited or transiently unavailable.
    /// </summary>
    /// <param name="request">The original synchronization request.</param>
    /// <param name="retryAt">The instant at which the retry should execute.</param>
    /// <param name="retryCount">The new retry count.</param>
    /// <param name="context">The current job execution context.</param>
    private Task EnqueueRetryAsync(
        ExternalActivitySyncJobRequest request,
        Instant retryAt,
        int retryCount,
        IJobExecutionContext context)
    {
        var retryKey = $"{request.IdempotencyKey}:retry:{retryAt.ToUnixTimeSeconds()}:{retryCount}";
        return backgroundJobClient.EnqueueExternalActivitySyncAsync(
            request with
            {
                IdempotencyKey = retryKey,
                NotBefore = retryAt,
                RetryCount = retryCount
            },
            context.CancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "External activity sync failed for {ProviderId}/{ExternalUserId}; scheduling infrastructure retry {RetryCount}")]
    private partial void LogRetry(Exception exception, string providerId, string externalUserId, int retryCount);
}
