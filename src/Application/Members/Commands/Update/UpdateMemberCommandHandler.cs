using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.Update;

public class UpdateMemberCommandHandler(IMemberRepository memberRepository, IClock clock) : ICommandHandler<UpdateMemberCommand>
{
    public Task Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return HandleAsync(request, cancellationToken);
    }

    private async Task HandleAsync(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(request.Id, cancellationToken)
                     ?? throw new ArgumentException($"Member with id {request.Id} not found");

        member.Update(
            request.FirstName,
            request.MiddleName ?? string.Empty,
            request.LastName,
            request.Description ?? string.Empty,
            request.PathAvatar ?? string.Empty,
            request.PreferredTimezone,
            clock
        );

        await memberRepository.UpdateAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);
    }
}
