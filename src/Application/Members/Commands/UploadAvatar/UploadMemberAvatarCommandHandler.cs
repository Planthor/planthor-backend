using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Application.Members.Commands.UploadAvatar;

public class UploadMemberAvatarCommandHandler : IRequestHandler<UploadMemberAvatarCommand, string>
{
    public Task<string> Handle(UploadMemberAvatarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
