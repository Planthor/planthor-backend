using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.Update;
using Domain.Members;
using Moq;
using NodaTime;

namespace Application.Tests.Members.Commands.Update;

public class UpdateMemberAvatarCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _mockRepository;
    private readonly Mock<IClock> _mockClock;
    private readonly UpdateMemberAvatarCommandHandler _handler;

    public UpdateMemberAvatarCommandHandlerTests()
    {
        _mockRepository = new Mock<IMemberRepository>();
        _mockClock = new Mock<IClock>();
        _mockClock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new UpdateMemberAvatarCommandHandler(_mockRepository.Object, _mockClock.Object);
    }

    private Member CreateMember() =>
        Member.Create("user1", "John", "", "Doe", "", "UTC", _mockClock.Object);

    [Fact]
    public async Task Handle_ExistingMember_UpdatesAvatar()
    {
        var member = CreateMember();
        var command = new UpdateMemberAvatarCommand(member.Id, "https://storage.example.com/new.jpg");
        _mockRepository
            .Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("https://storage.example.com/new.jpg", member.PathAvatar);
        _mockRepository.Verify(r => r.UpdateAsync(member, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MemberNotFound_Throws()
    {
        var memberId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateMemberAvatarCommand(memberId, "https://example.com/img.jpg"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MemberNotFound_DoesNotCallUpdate()
    {
        var memberId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateMemberAvatarCommand(memberId, "https://example.com/img.jpg"), CancellationToken.None));

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
