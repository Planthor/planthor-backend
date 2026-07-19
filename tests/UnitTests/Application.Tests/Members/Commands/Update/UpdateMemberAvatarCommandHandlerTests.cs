using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.Update;
using Domain.Members;
using NSubstitute;
using NodaTime;

namespace Application.Tests.Members.Commands.Update;

public class UpdateMemberAvatarCommandHandlerTests
{
    private readonly IMemberRepository _mockRepository;
    private readonly IClock _mockClock;
    private readonly UpdateMemberAvatarCommandHandler _handler;

    public UpdateMemberAvatarCommandHandlerTests()
    {
        _mockRepository = Substitute.For<IMemberRepository>();
        _mockClock = Substitute.For<IClock>();
        _mockClock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new UpdateMemberAvatarCommandHandler(_mockRepository, _mockClock);
    }

    private Member CreateMember() =>
        Member.Create("user1", "John", "", "Doe", "", "UTC", _mockClock);

    [Fact]
    public async Task Handle_ExistingMember_UpdatesAvatar()
    {
        var member = CreateMember();
        var command = new UpdateMemberAvatarCommand(member.Id, "https://storage.example.com/new.jpg");
        _mockRepository
            .GetByIdAsync(member.Id, Arg.Any<CancellationToken>())
            .Returns(member);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("https://storage.example.com/new.jpg", member.PathAvatar);
        _mockRepository.Received(1).UpdateAsync(member, Arg.Any<CancellationToken>());
        _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MemberNotFound_Throws()
    {
        var memberId = Guid.NewGuid();
        _mockRepository
            .GetByIdAsync(memberId, Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateMemberAvatarCommand(memberId, "https://example.com/img.jpg"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MemberNotFound_DoesNotCallUpdate()
    {
        var memberId = Guid.NewGuid();
        _mockRepository
            .GetByIdAsync(memberId, Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateMemberAvatarCommand(memberId, "https://example.com/img.jpg"), CancellationToken.None));

        _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
    }
}
