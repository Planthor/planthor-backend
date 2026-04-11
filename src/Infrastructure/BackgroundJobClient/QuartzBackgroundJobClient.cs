using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Quartz;

namespace Infrastructure.BackgroundJobClient;

public class QuartzBackgroundJobClient(ISchedulerFactory schedulerFactory) : IBackgroundJobClient
{
    public async Task EnqueueAvatarDownloadAsync(Guid memberId, Uri avatarUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(avatarUrl);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        // Your logic to bridge the Application need to the Infrastructure capability
        await scheduler.TriggerJob(new JobKey("DownloadAvatar"), new JobDataMap
        {
            { "MemberId", memberId.ToString() },
            { "Url", avatarUrl.ToString() }
        }, cancellationToken);
    }
}
