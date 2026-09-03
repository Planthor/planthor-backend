using System;
using System.Threading.Tasks;
using Application.ExternalSync.Commands.RevokeExternalConnectionByExternalUser;
using MediatR;
using Quartz;

namespace Infrastructure.BackgroundJobClient.Jobs;

/// <summary>Applies an external-provider deauthorization event through the Member aggregate.</summary>
/// <remarks>
/// Marked with <see cref="DisallowConcurrentExecutionAttribute"/> to prevent database concurrency
/// exceptions if multiple revocation triggers fire for the same connection simultaneously.
/// </remarks>
/// <param name="sender">The MediatR sender used to dispatch the revocation command.</param>
[DisallowConcurrentExecution]
public sealed class RevokeExternalConnectionJob(ISender sender) : IJob
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <c>ProviderId</c> or <c>ExternalUserId</c> is missing from the job data.</exception>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var providerId = context.MergedJobDataMap.GetString("ProviderId")
            ?? throw new InvalidOperationException("ProviderId is missing.");
        var externalUserId = context.MergedJobDataMap.GetString("ExternalUserId")
            ?? throw new InvalidOperationException("ExternalUserId is missing.");

        await _sender.Send(
            new RevokeExternalConnectionByExternalUserCommand(providerId, externalUserId),
            context.CancellationToken);
    }
}
