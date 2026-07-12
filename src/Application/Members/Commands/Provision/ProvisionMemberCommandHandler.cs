using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.Provision;

/// <summary>
/// Handles the provisioning of a new member or triggers avatar downloads for existing members.
/// Implements Just-In-Time (JIT) provisioning logic for users logging in via external providers.
/// </summary>
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

    /// <inheritdoc />
    public async Task<Guid> Handle(ProvisionMemberCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken);

        var memberToSave = existing ?? Member.Create(
            request.IdentifyName,
            request.FirstName,
            "", // Middle name not provided by external sources
            request.LastName,
            "JIT Provisioned",
            "UTC", //default timezone for JIT-provisioned accounts, can be updated by user
            _clock);

        if (existing is null)
        {
            await _memberRepository.AddAsync(memberToSave, cancellationToken);
        }

        bool hasChanges = existing is null;

        if (hasChanges)
        {
            await _memberRepository.SaveChangesAsync(cancellationToken);
        }

        // Always sync identities in the background (idempotent operation).
        await _backgroundJobClient.EnqueueIdentitySyncAsync(memberToSave.Id, request.IdentifyName, cancellationToken);

        // Enqueue avatar download if a URL was provided
        if (request.AvatarUrl is not null && memberToSave.PathAvatar is null)
        {
            await _backgroundJobClient.EnqueueAvatarDownloadAsync(memberToSave.Id, request.AvatarUrl, cancellationToken);
        }

        return memberToSave.Id;
    }
}
