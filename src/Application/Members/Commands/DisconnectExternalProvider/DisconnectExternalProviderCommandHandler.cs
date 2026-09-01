using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.DisconnectExternalProvider;

/// <summary>
/// Handles the disconnection of an external provider from a member.
/// </summary>
public sealed class DisconnectExternalProviderCommandHandler : ICommandHandler<DisconnectExternalProviderCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisconnectExternalProviderCommandHandler"/> class.
    /// </summary>
    public DisconnectExternalProviderCommandHandler(
        IMemberRepository memberRepository,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(memberRepository);
        ArgumentNullException.ThrowIfNull(clock);

        _memberRepository = memberRepository;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task Handle(DisconnectExternalProviderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task Core()
        {
            var member = await _memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken)
                ?? throw new KeyNotFoundException($"Member with Identity {request.IdentifyName} not found.");

            var provider = ExternalProvider.FromId(request.ProviderId);
            var connectionType = ExternalConnectionType.FromId(request.ConnectionTypeId);

            member.RevokeExternalProvider(provider, connectionType, _clock);

            await _memberRepository.UpdateAsync(member, cancellationToken);
            await _memberRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
