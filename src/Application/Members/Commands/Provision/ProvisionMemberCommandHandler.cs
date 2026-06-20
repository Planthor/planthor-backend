using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.Provision;

public class ProvisionMemberCommandHandler(
    IMemberRepository memberRepository,
    IClock clock,
    IBackgroundJobClient backgroundJobClient) : ICommandHandler<ProvisionMemberCommand, Guid>
{
    public async Task<Guid> Handle(ProvisionMemberCommand request, CancellationToken cancellationToken)
    {
        var existing = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken);

        // Member already exists — conditionally trigger avatar download if not yet stored
        if (existing is not null)
        {
            // Only enqueue if avatar hasn't been fetched yet and a URL was provided
            if (existing.PathAvatar is null && request.AvatarUrl is not null)
            {
                await backgroundJobClient.EnqueueAvatarDownloadAsync(existing.Id, request.AvatarUrl, cancellationToken);
            }

            return existing.Id;
        }

        // New member — create via JIT provisioning
        var member = Member.Create(
            request.IdentifyName,
            request.FirstName,
            "", // Middle name not provided by external sources
            request.LastName,
            "JIT Provisioned",
            "UTC", //default timezone for JIT-provisioned accounts, can be updated by user
            clock);

        await memberRepository.AddAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);

        // Enqueue avatar download if a URL was provided
        if (request.AvatarUrl is not null)
        {
            await backgroundJobClient.EnqueueAvatarDownloadAsync(member.Id, request.AvatarUrl, cancellationToken);
        }

        return member.Id;
    }
}
