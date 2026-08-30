using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.PersonalPlans.Commands.Create;
using Domain.Members;
using Domain.Plans;
using NodaTime;
using NSubstitute;

namespace Application.Tests.Members.PersonalPlans.Commands.Create;

public class CreatePersonalPlanCommandHandlerTests
{
    private readonly IMemberRepository _mockMemberRepository;
    private readonly IPlanRepository _mockPlanRepository;
    private readonly IClock _mockClock;
    private readonly CreatePersonalPlanCommandHandler _handler;

    public CreatePersonalPlanCommandHandlerTests()
    {
        _mockMemberRepository = Substitute.For<IMemberRepository>();
        _mockPlanRepository = Substitute.For<IPlanRepository>();
        _mockClock = Substitute.For<IClock>();
        _mockClock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new CreatePersonalPlanCommandHandler(
            _mockMemberRepository,
            _mockPlanRepository,
            _mockClock);
    }

    [Fact]
    public void Constructor_NullMemberRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CreatePersonalPlanCommandHandler(null!, _mockPlanRepository, _mockClock));
    }

    [Fact]
    public void Constructor_NullPlanRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CreatePersonalPlanCommandHandler(_mockMemberRepository, null!, _mockClock));
    }

    [Fact]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CreatePersonalPlanCommandHandler(_mockMemberRepository, _mockPlanRepository, null!));
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MemberNotFound_ThrowsArgumentException()
    {
        _mockMemberRepository.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>()).Returns((Member?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(new CreatePersonalPlanCommand("user1", "Plan 1", "km", 100, new DateTimeOffset(2024,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2025,1,1,0,0,0,TimeSpan.Zero), "2024-01-01", "2025-01-01", "UTC", true, true, 1, false, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesPlan()
    {
        var member = Member.Create("user1", "John", "", "Doe", "", "UTC", _mockClock);
        _mockMemberRepository.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>()).Returns(member);

        var result = await _handler.Handle(new CreatePersonalPlanCommand("user1", "Plan 1", "km", 100, new DateTimeOffset(2024,1,1,0,0,0,TimeSpan.Zero), new DateTimeOffset(2025,1,1,0,0,0,TimeSpan.Zero), "2024-01-01", "2025-01-01", "UTC", true, true, 1, false, null), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        
        await _mockPlanRepository.Received(1).AddAsync(Arg.Any<Plan>(), Arg.Any<CancellationToken>());
        await _mockPlanRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
