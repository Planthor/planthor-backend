using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.Create;

/// <summary>
/// Handles the creation of a new member.
/// </summary>
public class CreateMemberCommandHandler(
    IMemberRepository memberRepository,
    IClock clock) : ICommandHandler<CreateMemberCommand, Guid>
{

    public Task<Guid> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(memberRepository);

        return HandleAsync(request, cancellationToken);
    }

    private async Task<Guid> HandleAsync(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        var existing = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken);
        if (existing is not null){
            return existing.Id;
        }
        var member = Member.Create(
            request.IdentifyName,
            request.FirstName,
            request.MiddleName ?? string.Empty,
            request.LastName,
            request.Description ?? string.Empty,
            request.PreferredTimezone,
            clock);

        await memberRepository.AddAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);

        return member.Id;
    }
}
