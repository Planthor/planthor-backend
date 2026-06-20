using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.Provision;
using Application.Shared;
using Domain.Members;
using Moq;
using NodaTime;

namespace Application.Tests.Members.Commands.Provision;

public class ProvisionMemberCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _mockRepository;
    private readonly Mock<IClock> _mockClock;
    private readonly Mock<IBackgroundJobClient> _mockJobClient;
    private readonly ProvisionMemberCommandHandler _handler;

    public ProvisionMemberCommandHandlerTests()
    {
        _mockRepository = new Mock<IMemberRepository>();
        _mockClock = new Mock<IClock>();
        _mockJobClient = new Mock<IBackgroundJobClient>();

        _mockClock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new ProvisionMemberCommandHandler(
            _mockRepository.Object,
            _mockClock.Object,
            _mockJobClient.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_CreatesAndReturnsMemberId()
    {
        var command = new ProvisionMemberCommand("new_user", "John", "Doe", null);

        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(command.IdentifyName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member m, CancellationToken _) => m);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMemberAlreadyExists_ReturnsExistingMemberId()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock.Object);
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", null);

        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(command.IdentifyName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMember);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(existingMember.Id, result);
    }

    [Fact]
    public async Task Handle_WhenMemberAlreadyExists_DoesNotCreateDuplicate()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock.Object);
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", null);

        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(command.IdentifyName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMember);

        await _handler.Handle(command, CancellationToken.None);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNewMemberWithAvatarUrl_EnqueuesAvatarDownload()
    {
        var avatarUrl = new Uri("https://example.com/avatar.jpg");
        var command = new ProvisionMemberCommand("new_user", "John", "Doe", avatarUrl);

        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(command.IdentifyName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member m, CancellationToken _) => m);

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.Verify(
            j => j.EnqueueAvatarDownloadAsync(It.IsAny<Guid>(), avatarUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNewMemberWithNoAvatarUrl_DoesNotEnqueueAvatarDownload()
    {
        var command = new ProvisionMemberCommand("new_user", "John", "Doe", null);

        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(command.IdentifyName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member m, CancellationToken _) => m);

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.Verify(
            j => j.EnqueueAvatarDownloadAsync(It.IsAny<Guid>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExistingMemberWithNoAvatarAndUrlProvided_EnqueuesAvatarDownload()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock.Object);
        var avatarUrl = new Uri("https://example.com/avatar.jpg");
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", avatarUrl);

        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(command.IdentifyName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMember);

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.Verify(
            j => j.EnqueueAvatarDownloadAsync(existingMember.Id, avatarUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingMemberAlreadyHasAvatar_DoesNotEnqueueAvatarDownload()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock.Object);
        existingMember.UpdateAvatar("https://storage.example.com/stored-avatar.jpg", _mockClock.Object);

        var avatarUrl = new Uri("https://example.com/avatar.jpg");
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", avatarUrl);

        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(command.IdentifyName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMember);

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.Verify(
            j => j.EnqueueAvatarDownloadAsync(It.IsAny<Guid>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
