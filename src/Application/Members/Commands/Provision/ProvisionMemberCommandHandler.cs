using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.Provision;

public class ProvisionMemberCommandHandler : ICommandHandler<ProvisionMemberCommand, Guid>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IClock _clock;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ProvisionMemberCommandHandler(
        IMemberRepository memberRepository,
        IClock clock,
        IBackgroundJobClient backgroundJobClient)
    {
        ArgumentNullException.ThrowIfNull(memberRepository);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(backgroundJobClient);

        _memberRepository = memberRepository;
        _clock = clock;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<Guid> Handle(ProvisionMemberCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken);

        // Member already exists — conditionally trigger avatar download if not yet stored
        if (existing is not null)
        {
            // Only enqueue if avatar hasn't been fetched yet and a URL was provided
            if (existing.PathAvatar is null && request.AvatarUrl is not null)
            {
                await _backgroundJobClient.EnqueueAvatarDownloadAsync(existing.Id, request.AvatarUrl, cancellationToken);
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
            _clock);

        await _memberRepository.AddAsync(member, cancellationToken);
        await _memberRepository.SaveChangesAsync(cancellationToken);

        // Enqueue avatar download if a URL was provided
        if (request.AvatarUrl is not null)
        {
            await _backgroundJobClient.EnqueueAvatarDownloadAsync(member.Id, request.AvatarUrl, cancellationToken);
        }

        return member.Id;
    }
}
