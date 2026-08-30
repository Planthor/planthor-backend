using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Quartz;

namespace Infrastructure.BackgroundJobClient;

/// <summary>
/// Implementation of <see cref="IBackgroundJobClient"/> using Quartz.NET to schedule and execute background tasks.
/// </summary>
public sealed class QuartzBackgroundJobClient(ISchedulerFactory schedulerFactory) : IBackgroundJobClient
{
    /// <inheritdoc />
    public Task EnqueueAvatarDownloadAsync(Guid memberId, Uri avatarUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(avatarUrl);

        return Core();

        async Task Core()
        {
            var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

            // Your logic to bridge the Application need to the Infrastructure capability
            await scheduler.TriggerJob(new JobKey("DownloadAvatar"), new JobDataMap
            {
                { "MemberId", memberId.ToString() },
                { "Url", avatarUrl.ToString() }
            }, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task EnqueueIdentitySyncAsync(Guid memberId, string identifyName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifyName);

        return Core();

        async Task Core()
        {
            var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

            await scheduler.TriggerJob(new JobKey("SyncIdentity"), new JobDataMap
            {
                { "MemberId", memberId.ToString() },
                { "IdentifyName", identifyName }
            }, cancellationToken);
        }
    }
}
