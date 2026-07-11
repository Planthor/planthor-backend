using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Application.Members.Commands.UploadAvatar;

/// <summary>
/// Handles the <see cref="UploadMemberAvatarCommand"/> to generate and return a secure upload URL.
/// </summary>
public class UploadMemberAvatarCommandHandler : IRequestHandler<UploadMemberAvatarCommand, string>
{
    /// <inheritdoc />
    public Task<string> Handle(UploadMemberAvatarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
