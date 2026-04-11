using Application.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace Infrastructure.BackgroundJobClient;

public class QuartzBackgroundJobClient(IScheduler scheduler) : IBackgroundJobClient
{
    public async Task EnqueueAvatarDownloadAsync(Guid memberId, Uri avatarUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(avatarUrl);

        // Your logic to bridge the Application need to the Infrastructure capability
        await scheduler.TriggerJob(new JobKey("DownloadAvatar"), new JobDataMap
        {
            { "MemberId", memberId.ToString() },
            { "Url", avatarUrl.ToString() }
        }, cancellationToken);
    }
}
