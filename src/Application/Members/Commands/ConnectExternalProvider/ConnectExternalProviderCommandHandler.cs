using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.ConnectExternalProvider;

public sealed class ConnectExternalProviderCommandHandler(
    IMemberRepository memberRepository,
    IClock clock) : ICommandHandler<ConnectExternalProviderCommand>
{
    public async Task Handle(ConnectExternalProviderCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken)
            ?? throw new KeyNotFoundException($"Member with Identity {request.IdentifyName} not found.");

        var provider = ExternalProvider.FromId(request.ProviderId);
        var connectionType = ExternalConnectionType.FromId(request.ConnectionTypeId);

        member.ConnectExternalProvider(
            provider,
            connectionType,
            request.ExternalUserId,
            request.Scopes,
            clock);

        await memberRepository.UpdateAsync(member, cancellationToken);
    }
}
