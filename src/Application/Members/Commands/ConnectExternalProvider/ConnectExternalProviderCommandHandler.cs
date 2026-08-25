using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.ConnectExternalProvider;

/// <summary>
/// Handles the connection of an external provider to a member.
/// </summary>
public sealed class ConnectExternalProviderCommandHandler : ICommandHandler<ConnectExternalProviderCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectExternalProviderCommandHandler"/> class.
    /// </summary>
    public ConnectExternalProviderCommandHandler(
        IMemberRepository memberRepository,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(memberRepository);
        ArgumentNullException.ThrowIfNull(clock);

        _memberRepository = memberRepository;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task Handle(ConnectExternalProviderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task Core()
        {
            var member = await _memberRepository.GetByExternalIdentityAsync(ExternalProvider.Keycloak.Id, request.IdentifyName, cancellationToken)
                ?? throw new KeyNotFoundException($"Member with Identity {request.IdentifyName} not found.");

            var provider = ExternalProvider.FromId(request.ProviderId);
            var connectionType = ExternalConnectionType.FromId(request.ConnectionTypeId);

            member.ConnectExternalProvider(
                provider,
                connectionType,
                request.ExternalUserId,
                request.Scopes,
                _clock);

            await _memberRepository.UpdateAsync(member, cancellationToken);
            await _memberRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
