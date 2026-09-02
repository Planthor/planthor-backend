using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.ExternalSync.Commands.RevokeExternalConnectionByExternalUser;

/// <summary>
/// Applies provider-originated revocation through the Member aggregate.
/// </summary>
/// <param name="memberRepository">The repository for accessing member data.</param>
/// <param name="clock">The system clock used for audit stamping.</param>
public sealed class RevokeExternalConnectionByExternalUserCommandHandler(
    IMemberRepository memberRepository,
    IClock clock)
    : ICommandHandler<RevokeExternalConnectionByExternalUserCommand, bool>
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public async Task<bool> Handle(
        RevokeExternalConnectionByExternalUserCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await memberRepository.GetByActiveExternalConnectionAsync(
            request.ProviderId,
            ExternalConnectionType.ActivitiesSync.Id,
            request.ExternalUserId,
            cancellationToken);
        if (member is null)
        {
            return false;
        }

        member.RevokeExternalProvider(
            ExternalProvider.FromId(request.ProviderId),
            ExternalConnectionType.ActivitiesSync,
            clock);
        await memberRepository.UpdateAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
