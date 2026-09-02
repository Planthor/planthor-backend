using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.PersonalPlans.Queries.Details;
using Application.Tests.Shared;
using Domain.Members;
using Domain.Plans;
using NodaTime;
using NSubstitute;

namespace Application.Tests.Members.Queries.PersonalPlanDetails;

public class PersonalPlanDetailsQueryHandlerTests
{
    private readonly InMemoryReadOnlyContext _context;
    private readonly PersonalPlanDetailsQueryHandler _handler;
    private readonly IClock _mockClock;
    private readonly Instant _now = Instant.FromUtc(2026, 1, 1, 0, 0);

    public PersonalPlanDetailsQueryHandlerTests()
    {
        _context = new InMemoryReadOnlyContext();
        _handler = new PersonalPlanDetailsQueryHandler(_context);
        _mockClock = Substitute.For<IClock>();
        _mockClock.GetCurrentInstant().Returns(_now);
    }

    private void SetupContext(Member? member, Plan? plan)
    {
        _context.SetEntities<Member>(member is null ? [] : [member]);
        _context.SetEntities<Plan>(plan is null ? [] : [plan]);
    }

    [Fact]
    public async Task Handle_WhenPlanFound_ReturnsPersonalPlanDto()
    {
        var planId = Guid.NewGuid();
        var member = Member.Create("user1", "Alice", "", "Smith", "desc", "UTC", _mockClock);
        member.SubscribeToPlan(planId, true, 3, false, _mockClock);

        var plan = Plan.Create("My Plan", "km", 100, _now, _now.Plus(Duration.FromDays(30)), "2026-01-01", "2026-01-31", "UTC", true, _mockClock, member.Id);
        // We set the generated Id back to planId using reflection if needed, but since it's an entity, let's just use reflection to set Id
        typeof(Plan).GetProperty(nameof(Plan.Id))!.SetValue(plan, planId);

        SetupContext(member, plan);

        var result = await _handler.Handle(
            new PersonalPlanDetailsQuery("user1", planId),
            CancellationToken.None);

        Assert.Equal(planId, result.PlanId);
        Assert.Equal(member.Id, result.MemberId);
        Assert.True(result.DisplayOnProfile);
        Assert.Equal(3, result.Prioritize);
        Assert.False(result.LinkUserAdapters);
        Assert.Equal("My Plan", result.PlanName);
        Assert.Equal("km", result.Unit);
        Assert.Equal(100f, result.Target);
    }

    [Fact]
    public async Task Handle_WhenPersonalPlanNotFound_ThrowsKeyNotFoundException()
    {
        var planId = Guid.NewGuid();
        SetupContext(null, null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new PersonalPlanDetailsQuery("user1", planId), CancellationToken.None));

        Assert.Contains(planId.ToString(), ex.Message);
        Assert.Contains("user1", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenPlanEntityNotFound_ThrowsKeyNotFoundException()
    {
        var planId = Guid.NewGuid();
        var member = Member.Create("user1", "Alice", "", "Smith", "desc", "UTC", _mockClock);
        member.SubscribeToPlan(planId, true, 3, false, _mockClock);

        SetupContext(member, null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new PersonalPlanDetailsQuery("user1", planId), CancellationToken.None));

        Assert.Contains(planId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToContext()
    {
        var cancellationToken = new CancellationToken(canceled: false);
        SetupContext(null, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new PersonalPlanDetailsQuery("user1", Guid.NewGuid()), cancellationToken));

        Assert.Equal(cancellationToken, _context.LastCancellationToken);
    }

    [Fact]
    public async Task Handle_ExecutesQueryLambda_ProjectsToDto()
    {
        var member = Member.Create("alice", "Alice", "", "Smith", "desc", "UTC", _mockClock);
        member.ConnectExternalProvider(ExternalProvider.Keycloak, ExternalConnectionType.Identity, "alice", [], _mockClock);
        var planId = Guid.NewGuid();
        member.SubscribeToPlan(planId, true, 1, false, _mockClock);

        var plan = Plan.Create("My Plan", "km", 100, _now, _now.Plus(Duration.FromDays(30)), "2026-01-01", "2026-01-31", "UTC", true, _mockClock, member.Id);
        typeof(Plan).GetProperty(nameof(Plan.Id))!.SetValue(plan, planId);

        SetupContext(member, plan);

        var result = await _handler.Handle(new PersonalPlanDetailsQuery("alice", planId), CancellationToken.None);

        Assert.Equal(planId, result.PlanId);
        Assert.Equal(member.Id, result.MemberId);
        Assert.True(result.DisplayOnProfile);
        Assert.Equal(1, result.Prioritize);
        Assert.False(result.LinkUserAdapters);
        Assert.Equal("My Plan", result.PlanName);
    }
}
