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

        if (existing is not null)
        {
            if (existing.PathAvatar is null && request.AvatarUrl is not null)
            {
                await backgroundJobClient.EnqueueAvatarDownloadAsync(existing.Id, request.AvatarUrl, cancellationToken);
            }

            return existing.Id;
        }

        var member = Member.Create(
            request.IdentifyName,
            request.FirstName,
            "",
            request.LastName,
            "JIT Provisioned",
            "UTC",
            clock);

        await memberRepository.AddAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);

        if (request.AvatarUrl is not null)
        {
            await backgroundJobClient.EnqueueAvatarDownloadAsync(member.Id, request.AvatarUrl, cancellationToken);
        }

        return member.Id;
    }
}
