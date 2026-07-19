using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.Create;
using Domain.Members;
using NSubstitute;
using NodaTime;

namespace Application.Tests.Members.Commands.Create;

public class CreateMemberCommandHandlerTests
{
    private readonly IMemberRepository _mockRepository;
    private readonly IClock _mockClock;
    private readonly CreateMemberCommandHandler _handler;

    public CreateMemberCommandHandlerTests()
    {
        _mockRepository = Substitute.For<IMemberRepository>();
        _mockClock = Substitute.For<IClock>();
        _mockClock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new CreateMemberCommandHandler(_mockRepository, _mockClock);
    }

    private static CreateMemberCommand ValidCommand(string identifyName = "user1") =>
        new(identifyName, "John", null, "Doe", null, "UTC");

    [Fact]
    public async Task Handle_NewMember_CreatesAndReturnsId()
    {
        var command = ValidCommand();
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
    public async Task Handle_ExistingMember_ReturnsExistingId()
    {
        var command = ValidCommand("existing");
        var existing = Member.Create("existing", "Jane", "", "Doe", "", "UTC", _mockClock);
        _mockRepository
            .GetByIdentifyNameAsync("existing", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(existing.Id, result);
        _mockRepository.DidNotReceive().AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewMember_UsesMiddleNameEmpty_WhenNull()
    {
        var command = new CreateMemberCommand("user1", "John", null, "Doe", null, "UTC");
        _mockRepository
            .GetByIdentifyNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Member?)null);
        Member? captured = null;
        _mockRepository
            .AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>())
            .Returns(c => 
            {
                captured = c.ArgAt<Member>(0);
                return Task.FromResult(c.ArgAt<Member>(0));
            });

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
            .GetByIdentifyNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Member?)null);
        _mockRepository
            .AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>())
            .Returns(c => 
            {
                captured = c.ArgAt<Member>(0);
                return Task.FromResult(c.ArgAt<Member>(0));
            });

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(string.Empty, captured.Description);
    }
}
