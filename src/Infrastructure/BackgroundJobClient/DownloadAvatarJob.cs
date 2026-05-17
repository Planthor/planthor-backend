using System;
using System.Net.Http;
using System.Threading.Tasks;
using Application.Members.Commands.Update;
using Application.Shared;
using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Infrastructure.BackgroundJobClient;

/// <summary>
/// A Quartz background job responsible for downloading a member's avatar from an external URL
/// and uploading it to the platform's internal storage.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients to download the image.</param>
/// <param name="avatarStorageService">The service used to upload the avatar to storage.</param>
/// <param name="sender">The MediatR sender used to update the member's profile with the new path.</param>
/// <param name="logger">The logger for tracking job execution and errors.</param>
[DisallowConcurrentExecution]
public class DownloadAvatarJob(
    IHttpClientFactory httpClientFactory,
    IAvatarStorageService avatarStorageService,
    ISender sender,
    ILogger<DownloadAvatarJob> logger) : IJob
{
    /// <summary>
    /// Executes the avatar download and storage process.
    /// </summary>
    /// <param name="context">The execution context provided by Quartz, containing the MemberId and Url.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="JobExecutionException">Thrown when the download or upload process fails.</exception>
    public async Task Execute(IJobExecutionContext context)
    {
        var memberIdString = context.MergedJobDataMap.GetString("MemberId");
        var urlString = context.MergedJobDataMap.GetString("Url");

        if (string.IsNullOrEmpty(memberIdString) || string.IsNullOrEmpty(urlString))
        {
            logger.LogWarning("Invalid job data: MemberId={MemberId}, Url={Url}", memberIdString, urlString);
            return;
        }

        var memberId = Guid.Parse(memberIdString);
        var url = new Uri(urlString);

        try
        {
            using var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(url, context.CancellationToken);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            using var stream = await response.Content.ReadAsStreamAsync(context.CancellationToken);

            var avatarPath = await avatarStorageService.UploadAvatarAsync(
                memberId,
                stream,
                contentType,
                context.CancellationToken);

            await sender.Send(new UpdateMemberAvatarCommand(memberId, avatarPath), context.CancellationToken);

            logger.LogInformation("Successfully updated avatar for member {MemberId}", memberId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download or upload avatar for member {MemberId}", memberId);
            throw new JobExecutionException(msg: "Failed to process avatar download", cause: ex, refireImmediately: false);
        }
    }
}
