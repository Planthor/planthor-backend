using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.PersonalPlans.Queries.List;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using Moq;
using NodaTime;

namespace Application.Tests.Members.Queries.ListPersonalPlans;

public class ListPersonalPlansQueryHandlerTests
{
    private readonly Mock<IReadOnlyContext> _mockContext;
    private readonly ListPersonalPlansQueryHandler _handler;
    private readonly IClock _clock;
    private readonly Instant _from;
    private readonly Instant _to;

    public ListPersonalPlansQueryHandlerTests()
    {
        _mockContext = new Mock<IReadOnlyContext>();
        _handler = new ListPersonalPlansQueryHandler(_mockContext.Object);
        _clock = Mock.Of<IClock>(c => c.GetCurrentInstant() == Instant.FromUtc(2024, 1, 1, 0, 0));
        _from = Instant.FromUtc(2024, 1, 1, 0, 0);
        _to = Instant.FromUtc(2024, 12, 31, 0, 0);
    }

    private Plan CreatePlan(string name = "Test Plan") =>
        Plan.Create(name, "km", 100f, _from, _to, "2024-01-01", "2024-12-31", "UTC", true, _clock, Guid.NewGuid());

    private Member CreateMember(string identifyName = "user1") =>
        Member.Create(identifyName, "John", "", "Doe", "", "UTC", _clock);

    private void SetupMemberContext(Member? member)
    {
        _mockContext
            .Setup(c => c.FirstOrDefaultAsync<Member, Member>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<Member>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
    }

    private void SetupPlansContext(List<Plan> plans)
    {
        _mockContext
            .Setup(c => c.QueryAsync<Plan, Plan>(
                It.IsAny<Func<IQueryable<Plan>, IQueryable<Plan>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_ReturnsEmptyResult()
    {
        SetupMemberContext(null);

        var result = await _handler.Handle(new ListPersonalPlansQuery("unknown_user"), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task Handle_WhenMemberHasNoPersonalPlans_ReturnsEmptyResult()
    {
        SetupMemberContext(CreateMember());

        var result = await _handler.Handle(new ListPersonalPlansQuery("user1"), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task Handle_WhenMemberHasPlans_ReturnsMappedDtos()
    {
        var plan = CreatePlan("Run 100km");
        var member = CreateMember();
        member.SubscribeToPlan(plan.Id, displayOnProfile: true, prioritize: 5, linkUserAdapter: false, _clock);

        SetupMemberContext(member);
        SetupPlansContext([plan]);

        var result = await _handler.Handle(new ListPersonalPlansQuery("user1"), CancellationToken.None);

        var dto = Assert.Single(result.Items);
        Assert.Equal(plan.Id, dto.PlanId);
        Assert.Equal(member.Id, dto.MemberId);
        Assert.True(dto.DisplayOnProfile);
        Assert.Equal(5, dto.Prioritize);
        Assert.False(dto.LinkUserAdapters);
        Assert.Equal("Run 100km", dto.PlanName);
        Assert.Equal("km", dto.Unit);
        Assert.Equal(100f, dto.Target);
        Assert.Equal(0f, dto.CurrentValue);
        Assert.Equal(0.0, dto.ProgressPercentage);
        Assert.False(dto.Completed);
    }

    [Fact]
    public async Task Handle_WhenResultFitsOnOnePage_SetsHasNextPageFalse()
    {
        var plan1 = CreatePlan("Plan A");
        var plan2 = CreatePlan("Plan B");
        var member = CreateMember();
        member.SubscribeToPlan(plan1.Id, displayOnProfile: true, prioritize: 1, linkUserAdapter: false, _clock);
        member.SubscribeToPlan(plan2.Id, displayOnProfile: false, prioritize: 2, linkUserAdapter: false, _clock);

        SetupMemberContext(member);
        SetupPlansContext([plan1, plan2]);

        var result = await _handler.Handle(new ListPersonalPlansQuery("user1", Limit: 10), CancellationToken.None);

        Assert.Equal(2, result.Items.Count());
        Assert.False(result.HasNextPage);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task Handle_WhenPlansExceedLimit_SetsHasNextPageTrueAndNextCursor()
    {
        var plan1 = CreatePlan("Plan A");
        var plan2 = CreatePlan("Plan B");
        var member = CreateMember();
        member.SubscribeToPlan(plan1.Id, displayOnProfile: true, prioritize: 1, linkUserAdapter: false, _clock);
        member.SubscribeToPlan(plan2.Id, displayOnProfile: false, prioritize: 2, linkUserAdapter: false, _clock);

        // Determine the first plan in OrderBy(PlanId) order
        var orderedPlans = new[] { plan1, plan2 }.OrderBy(p => p.Id).ToList();
        var firstPlan = orderedPlans[0];

        SetupMemberContext(member);
        SetupPlansContext([firstPlan]);

        var result = await _handler.Handle(new ListPersonalPlansQuery("user1", Limit: 1), CancellationToken.None);

        Assert.True(result.HasNextPage);
        Assert.Equal(firstPlan.Id.ToString(), result.NextCursor);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Handle_WhenCursorSet_SkipsPlansAtOrBeforeCursor()
    {
        var plan1 = CreatePlan("Plan A");
        var plan2 = CreatePlan("Plan B");
        var member = CreateMember();
        member.SubscribeToPlan(plan1.Id, displayOnProfile: true, prioritize: 1, linkUserAdapter: false, _clock);
        member.SubscribeToPlan(plan2.Id, displayOnProfile: false, prioritize: 2, linkUserAdapter: false, _clock);

        // Determine ordering: cursor = the first PlanId after ordering → only second plan passes
        var orderedIds = new[] { plan1.Id, plan2.Id }.OrderBy(id => id).ToList();
        var cursorId = orderedIds[0];
        var expectedPlan = plan1.Id == orderedIds[1] ? plan1 : plan2;

        SetupMemberContext(member);
        SetupPlansContext([expectedPlan]);

        var result = await _handler.Handle(new ListPersonalPlansQuery("user1", Cursor: cursorId), CancellationToken.None);

        var dto = Assert.Single(result.Items);
        Assert.Equal(expectedPlan.Id, dto.PlanId);
    }

    [Fact]
    public async Task Handle_WhenStatusFilterMatchesPlans_ReturnsDtos()
    {
        var plan = CreatePlan("Planned Run");
        var member = CreateMember();
        member.SubscribeToPlan(plan.Id, displayOnProfile: true, prioritize: 1, linkUserAdapter: false, _clock);

        SetupMemberContext(member);
        SetupPlansContext([plan]);

        // Plan.Create defaults to PlanStatus.Planned (Id="P", Name="PLANNED")
        var result = await _handler.Handle(
            new ListPersonalPlansQuery("user1", Statuses: ["PLANNED"]),
            CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Handle_WhenStatusFilterExcludesAllPlans_ReturnsEmptyItems()
    {
        var plan = CreatePlan("Planned Run");
        var member = CreateMember();
        member.SubscribeToPlan(plan.Id, displayOnProfile: true, prioritize: 1, linkUserAdapter: false, _clock);

        SetupMemberContext(member);
        SetupPlansContext([plan]);

        // All plans are Planned; filtering by Active excludes them
        var result = await _handler.Handle(
            new ListPersonalPlansQuery("user1", Statuses: ["Active"]),
            CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Handle_WhenStatusFilterIsInvalid_ReturnsAllPlans()
    {
        var plan = CreatePlan("Run");
        var member = CreateMember();
        member.SubscribeToPlan(plan.Id, displayOnProfile: true, prioritize: 1, linkUserAdapter: false, _clock);

        SetupMemberContext(member);
        SetupPlansContext([plan]);

        // Unrecognized status → validStatusIds is empty → no filter applied
        var result = await _handler.Handle(
            new ListPersonalPlansQuery("user1", Statuses: ["BOGUS_STATUS"]),
            CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToContext()
    {
        SetupMemberContext(null);
        var cancellationToken = new CancellationToken(canceled: false);

        await _handler.Handle(new ListPersonalPlansQuery("user1"), cancellationToken);

        _mockContext.Verify(
            c => c.FirstOrDefaultAsync<Member, Member>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<Member>>>(),
                cancellationToken),
            Times.Once);
    }
}
