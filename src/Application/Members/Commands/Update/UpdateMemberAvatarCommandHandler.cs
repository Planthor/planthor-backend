using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.Update;

/// <summary>
/// Handles the <see cref="UpdateMemberAvatarCommand"/> to update a member's avatar path.
/// </summary>
/// <param name="memberRepository">The repository used to fetch and update member data.</param>
/// <param name="clock">The system clock used for audit stamping.</param>
public class UpdateMemberAvatarCommandHandler(
    IMemberRepository memberRepository,
    IClock clock) : ICommandHandler<UpdateMemberAvatarCommand>
{
    /// <summary>
    /// Processes the update of a member's avatar path.
    /// </summary>
    /// <param name="request">The command containing the member ID and new avatar path.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the completion of the update.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the specified member is not found.</exception>
    public async Task Handle(UpdateMemberAvatarCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new InvalidOperationException($"Member with ID {request.MemberId} not found.");

        member.UpdateAvatar(request.AvatarPath, clock);

        await memberRepository.UpdateAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);
    }
}
