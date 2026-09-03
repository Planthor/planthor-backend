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
    private readonly IMemberRepository _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public Task<bool> Handle(
        RevokeExternalConnectionByExternalUserCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<bool> Core()
        {
            var member = await _memberRepository.GetByActiveExternalConnectionAsync(
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
                _clock);
            await _memberRepository.UpdateAsync(member, cancellationToken);
            await _memberRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
