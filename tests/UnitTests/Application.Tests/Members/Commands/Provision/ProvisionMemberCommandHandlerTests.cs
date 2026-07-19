using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.Provision;
using Application.Shared;
using Domain.Members;
using NSubstitute;
using NodaTime;

namespace Application.Tests.Members.Commands.Provision;

public class ProvisionMemberCommandHandlerTests
{
    private readonly IMemberRepository _mockRepository;
    private readonly IClock _mockClock;
    private readonly IBackgroundJobClient _mockJobClient;
    private readonly ProvisionMemberCommandHandler _handler;

    public ProvisionMemberCommandHandlerTests()
    {
        _mockRepository = Substitute.For<IMemberRepository>();
        _mockClock = Substitute.For<IClock>();
        _mockJobClient = Substitute.For<IBackgroundJobClient>();

        _mockClock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new ProvisionMemberCommandHandler(
            _mockRepository,
            _mockClock,
            _mockJobClient);
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_CreatesAndReturnsMemberId()
    {
        var command = new ProvisionMemberCommand("new_user", "John", "Doe", null);

        _mockRepository
            .GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        _mockRepository
            .AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>())
            .Returns(c => Task.FromResult(c.ArgAt<Member>(0)));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        _mockRepository.Received(1).AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
        _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMemberAlreadyExists_ReturnsExistingMemberId()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock);
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", null);

        _mockRepository
            .GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns(existingMember);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(existingMember.Id, result);
    }

    [Fact]
    public async Task Handle_WhenMemberAlreadyExists_DoesNotCreateDuplicate()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock);
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", null);

        _mockRepository
            .GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns(existingMember);

        await _handler.Handle(command, CancellationToken.None);

        _mockRepository.DidNotReceive().AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
        _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNewMemberWithAvatarUrl_EnqueuesAvatarDownload()
    {
        var avatarUrl = new Uri("https://example.com/avatar.jpg");
        var command = new ProvisionMemberCommand("new_user", "John", "Doe", avatarUrl);

        _mockRepository
            .GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        _mockRepository
            .AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>())
            .Returns(c => Task.FromResult(c.ArgAt<Member>(0)));

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.Received(1).EnqueueAvatarDownloadAsync(Arg.Any<Guid>(), avatarUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNewMemberWithNoAvatarUrl_DoesNotEnqueueAvatarDownload()
    {
        var command = new ProvisionMemberCommand("new_user", "John", "Doe", null);

        _mockRepository
            .GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        _mockRepository
            .AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>())
            .Returns(c => Task.FromResult(c.ArgAt<Member>(0)));

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.DidNotReceive().EnqueueAvatarDownloadAsync(Arg.Any<Guid>(), Arg.Any<Uri>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExistingMemberWithNoAvatarAndUrlProvided_EnqueuesAvatarDownload()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock);
        var avatarUrl = new Uri("https://example.com/avatar.jpg");
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", avatarUrl);

        _mockRepository
            .GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns(existingMember);

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.Received(1).EnqueueAvatarDownloadAsync(existingMember.Id, avatarUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExistingMemberAlreadyHasAvatar_DoesNotEnqueueAvatarDownload()
    {
        var existingMember = Member.Create("existing_user", "Jane", "", "Doe", "JIT Provisioned", "UTC", _mockClock);
        existingMember.UpdateAvatar("https://storage.example.com/stored-avatar.jpg", _mockClock);

        var avatarUrl = new Uri("https://example.com/avatar.jpg");
        var command = new ProvisionMemberCommand("existing_user", "Jane", "Doe", avatarUrl);

        _mockRepository
            .GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns(existingMember);

        await _handler.Handle(command, CancellationToken.None);

        _mockJobClient.DidNotReceive().EnqueueAvatarDownloadAsync(Arg.Any<Guid>(), Arg.Any<Uri>(), Arg.Any<CancellationToken>());
    }
}
