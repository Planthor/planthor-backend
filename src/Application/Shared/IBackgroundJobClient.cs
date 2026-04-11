using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared;

/// <summary>
/// Abstraction for enqueuing background tasks to avoid coupling application logic to specific infrastructure like Quartz or Hangfire.
/// </summary>
public interface IBackgroundJobClient
{
    /// <summary>
    /// Schedules a background job to download and process a member's avatar from an external URL.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member.</param>
    /// <param name="avatarUrl">The URL of the avatar image to be downloaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task EnqueueAvatarDownloadAsync(Guid memberId, Uri avatarUrl, CancellationToken cancellationToken);
}
