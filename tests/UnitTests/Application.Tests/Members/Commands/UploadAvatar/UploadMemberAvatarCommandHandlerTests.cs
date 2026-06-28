using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.UploadAvatar;
using Xunit;

namespace Application.Tests.Members.Commands.UploadAvatar;

public class UploadMemberAvatarCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotYetImplemented_ThrowsNotImplementedException()
    {
        var handler = new UploadMemberAvatarCommandHandler();
        var command = new UploadMemberAvatarCommand();

        await Assert.ThrowsAsync<NotImplementedException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
