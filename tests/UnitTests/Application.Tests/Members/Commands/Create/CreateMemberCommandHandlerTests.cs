using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.Create;
using Domain.Members;
using Moq;
using NodaTime;

namespace Application.Tests.Members.Commands.Create;

public class CreateMemberCommandHandlerTests
{
    private readonly Mock<IMemberRepository> _mockRepository;
    private readonly Mock<IClock> _mockClock;
    private readonly CreateMemberCommandHandler _handler;

    public CreateMemberCommandHandlerTests()
    {
        _mockRepository = new Mock<IMemberRepository>();
        _mockClock = new Mock<IClock>();
        _mockClock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new CreateMemberCommandHandler(_mockRepository.Object, _mockClock.Object);
    }

    private static CreateMemberCommand ValidCommand(string identifyName = "user1") =>
        new(identifyName, "John", null, "Doe", null, "UTC");

    [Fact]
    public async Task Handle_NewMember_CreatesAndReturnsId()
    {
        var command = ValidCommand();
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
    public async Task Handle_ExistingMember_ReturnsExistingId()
    {
        var command = ValidCommand("existing");
        var existing = Member.Create("existing", "Jane", "", "Doe", "", "UTC", _mockClock.Object);
        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(existing.Id, result);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewMember_UsesMiddleNameEmpty_WhenNull()
    {
        var command = new CreateMemberCommand("user1", "John", null, "Doe", null, "UTC");
        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member m, CancellationToken _) => m);

        Member? captured = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
            .Callback<Member, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync((Member m, CancellationToken _) => m);

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(string.Empty, captured.MiddleName);
    }

    [Fact]
    public async Task Handle_NewMember_UsesDescriptionEmpty_WhenNull()
    {
        var command = new CreateMemberCommand("user1", "John", null, "Doe", null, "UTC");
        Member? captured = null;
        _mockRepository
            .Setup(r => r.GetByIdentifyNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
            .Callback<Member, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync((Member m, CancellationToken _) => m);

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(string.Empty, captured.Description);
    }
}
