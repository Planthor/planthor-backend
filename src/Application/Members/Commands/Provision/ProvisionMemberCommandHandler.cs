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
        // Reuses Domain logic to ensure consistency
        var member = Member.Create(
            request.IdentifyName,
            request.FirstName,
            "", // MiddleName
            request.LastName,
            "JIT Provisioned",
            "UTC", // Default timezone
            clock);


        await memberRepository.AddAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);

        if (request.AvatarUrl != null)
        {
            await backgroundJobClient.EnqueueAvatarDownloadAsync(member.Id, request.AvatarUrl, cancellationToken);
        }

        return member.Id;
    }
}
