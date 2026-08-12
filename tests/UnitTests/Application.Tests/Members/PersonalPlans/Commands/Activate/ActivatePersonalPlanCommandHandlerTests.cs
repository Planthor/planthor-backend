using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.PersonalPlans.Commands.Activate;
using Domain.Members;
using Domain.Plans;
using NodaTime;
using NSubstitute;

namespace Application.Tests.Members.PersonalPlans.Commands.Activate;

public class ActivatePersonalPlanCommandHandlerTests
{
    private readonly IMemberRepository _mockMemberRepository;
    private readonly IPlanRepository _mockPlanRepository;
    private readonly IClock _mockClock;
    private readonly ActivatePersonalPlanCommandHandler _handler;

    public ActivatePersonalPlanCommandHandlerTests()
    {
        _mockMemberRepository = Substitute.For<IMemberRepository>();
        _mockPlanRepository = Substitute.For<IPlanRepository>();
        _mockClock = Substitute.For<IClock>();
        _mockClock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 1, 1, 0, 0));

        _handler = new ActivatePersonalPlanCommandHandler(
            _mockMemberRepository,
            _mockPlanRepository,
            _mockClock);
    }

    [Fact]
    public void Constructor_NullMemberRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ActivatePersonalPlanCommandHandler(null!, _mockPlanRepository, _mockClock));
    }

    [Fact]
    public void Constructor_NullPlanRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ActivatePersonalPlanCommandHandler(_mockMemberRepository, null!, _mockClock));
    }

    [Fact]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ActivatePersonalPlanCommandHandler(_mockMemberRepository, _mockPlanRepository, null!));
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MemberNotFound_ThrowsKeyNotFoundException()
    {
        _mockMemberRepository.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>()).Returns((Member?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(new ActivatePersonalPlanCommand("user1", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PersonalPlanNotFound_ThrowsKeyNotFoundException()
    {
        var member = Member.Create("user1", "John", "", "Doe", "", "UTC", _mockClock);
        _mockMemberRepository.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>()).Returns(member);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(new ActivatePersonalPlanCommand("user1", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PlanNotFound_ThrowsKeyNotFoundException()
    {
        var planId = Guid.NewGuid();
        var member = Member.Create("user1", "John", "", "Doe", "", "UTC", _mockClock);
        member.SubscribeToPlan(planId, true, 0, false, _mockClock);
        
        _mockMemberRepository.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>()).Returns(member);
        _mockPlanRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns((Plan?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(new ActivatePersonalPlanCommand("user1", planId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ActivatesPlan()
    {
        var planId = Guid.NewGuid();
        var member = Member.Create("user1", "John", "", "Doe", "", "UTC", _mockClock);
        member.SubscribeToPlan(planId, true, 0, false, _mockClock);
        
        var plan = Plan.Create("Plan 1", "km", 100, Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2025, 1, 1, 0, 0), "2024-01-01", "2025-01-01", "UTC", true, _mockClock, Guid.NewGuid());
        typeof(Plan).GetProperty("Id")!.SetValue(plan, planId);

        _mockMemberRepository.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>()).Returns(member);
        _mockPlanRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await _handler.Handle(new ActivatePersonalPlanCommand("user1", planId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(planId, result.PlanId);
        Assert.Equal(member.Id, result.MemberId);
        await _mockPlanRepository.Received(1).UpdateAsync(plan, Arg.Any<CancellationToken>());
        await _mockPlanRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithZeroTarget_ReturnsZeroProgress()
    {
        var planId = Guid.NewGuid();
        var member = Member.Create("user1", "John", "", "Doe", "", "UTC", _mockClock);
        member.SubscribeToPlan(planId, true, 0, false, _mockClock);
        
        var plan = Plan.Create("Plan 1", "km", 100, Instant.FromUtc(2024, 1, 1, 0, 0), Instant.FromUtc(2025, 1, 1, 0, 0), "2024-01-01", "2025-01-01", "UTC", true, _mockClock, Guid.NewGuid());
        typeof(Plan).GetProperty("Id")!.SetValue(plan, planId);
        typeof(Plan).GetProperty("Target")!.SetValue(plan, 0f);

        _mockMemberRepository.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>()).Returns(member);
        _mockPlanRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await _handler.Handle(new ActivatePersonalPlanCommand("user1", planId), CancellationToken.None);

        Assert.Equal(0, result.ProgressPercentage);
    }
}
