using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.ExternalSync.Commands.ProcessExternalActivitySync;
using Application.Interfaces;
using Application.Shared;
using Domain.Members;
using Domain.Plans;
using NodaTime;
using NSubstitute;

namespace Application.Tests.ExternalSync.Commands.ProcessExternalActivitySync;

public sealed class ProcessExternalActivitySyncCommandHandlerTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 1, 15, 12, 0);
    private readonly IMemberRepository _memberRepository = Substitute.For<IMemberRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IActivitySyncAdapter _adapter = Substitute.For<IActivitySyncAdapter>();
    private readonly IClock _clock = new TestClock(Now);

    public ProcessExternalActivitySyncCommandHandlerTests()
    {
        _adapter.ProviderId.Returns(ExternalProvider.Strava.Id);
    }

    [Fact]
    public async Task Handle_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!, CancellationToken.None));

        // Assert
        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task Handle_WithoutMatchingAdapter_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler([]);
        var command = CreateCommand();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Contains("No activity adapter", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_WhenConnectionNoLongerExists_ReturnsNoOp()
    {
        // Arrange
        _memberRepository.GetByActiveExternalConnectionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((Member?)null);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(0, result.LogsCreated);
        await _adapter.DidNotReceiveWithAnyArgs()
            .MarkRunningAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WithoutEligiblePlans_MarksRunSucceededWithoutFetching()
    {
        // Arrange
        var member = CreateMember();
        var unlinkedPlan = CreateSportPlan();
        member.SubscribeToPlan(unlinkedPlan.Id, true, 0, false, _clock);
        _memberRepository.GetByActiveExternalConnectionAsync(
                ExternalProvider.Strava.Id,
                ExternalConnectionType.ActivitiesSync.Id,
                "42",
                Arg.Any<CancellationToken>())
            .Returns(member);
        _planRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(0, result.LogsCreated);
        await _adapter.Received(1).MarkRunningAsync("42", "initial", Arg.Any<CancellationToken>());
        await _adapter.Received(1).MarkSucceededAsync(
            "42",
            "initial",
            null,
            0,
            Arg.Any<CancellationToken>());
        await _adapter.DidNotReceiveWithAnyArgs()
            .FetchActivitiesAsync(default!, default, default, default);
    }

    [Theory]
    [InlineData("N", null, false, null)]
    [InlineData("A", null, false, "external_authorization_required")]
    [InlineData("R", null, true, "external_rate_limited")]
    [InlineData("T", "provider_unavailable", true, "provider_unavailable")]
    public async Task Handle_WithNonSuccessFetch_RecordsExpectedOperationalState(
        string outcomeId,
        string? providedErrorCode,
        bool isDeferred,
        string? expectedErrorCode)
    {
        // Arrange
        var (_, _) = ConfigureEligibleRun();
        var outcome = ActivitySyncOutcome.FromId(outcomeId);
        var suppliedRetryAt = outcome == ActivitySyncOutcome.TransientFailure
            ? Now.Plus(Duration.FromMinutes(2))
            : (Instant?)null;
        _adapter.FetchActivitiesAsync(
                "42",
                Arg.Any<Instant>(),
                Now,
                Arg.Any<CancellationToken>())
            .Returns(new ActivitySyncFetchResultDto(
                outcome,
                [],
                RetryAt: suppliedRetryAt,
                ErrorCode: providedErrorCode));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(0, result.LogsCreated);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        if (outcome == ActivitySyncOutcome.NotFound)
        {
            Assert.Null(result.RetryAt);
            await _adapter.Received(1).MarkSucceededAsync(
                "42", "initial", null, 0, Arg.Any<CancellationToken>());
        }
        else if (isDeferred)
        {
            var expectedRetryAt = suppliedRetryAt ?? Now.Plus(Duration.FromMinutes(5));
            Assert.Equal(expectedRetryAt, result.RetryAt);
            await _adapter.Received(1).MarkDeferredAsync(
                "42",
                "initial",
                expectedRetryAt,
                expectedErrorCode!,
                Arg.Any<CancellationToken>());
        }
        else
        {
            Assert.Null(result.RetryAt);
            await _adapter.Received(1).MarkFailedAsync(
                "42",
                "initial",
                expectedErrorCode!,
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Handle_WithHistoricalSuccess_UsesEarliestRangeAndCommitsWatermark()
    {
        // Arrange
        var (member, eligiblePlan) = ConfigureEligibleRun();
        var earlierPlan = CreateSportPlan(
            Instant.FromUtc(2025, 12, 1, 0, 0),
            Instant.FromUtc(2026, 1, 31, 23, 59));
        earlierPlan.Activate(member.Id, _clock);
        member.SubscribeToPlan(earlierPlan.Id, true, 1, true, _clock);
        _planRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([eligiblePlan, earlierPlan]);
        _adapter.FetchActivitiesAsync(
                "42",
                earlierPlan.From,
                Now,
                Arg.Any<CancellationToken>())
            .Returns(new ActivitySyncFetchResultDto(
                ActivitySyncOutcome.Success,
                [],
                WatermarkCandidate: Now));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(0, result.LogsCreated);
        await _planRepository.DidNotReceiveWithAnyArgs()
            .UpdateAsync(default!, default);
        await _planRepository.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(default);
        await _adapter.Received(1).MarkSucceededAsync(
            "42", "initial", Now, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWebhookActivities_FiltersInvalidDataAndCreatesIdempotentLog()
    {
        // Arrange
        var (_, eligiblePlan) = ConfigureEligibleRun();
        var inactiveLinkedPlan = CreateSportPlan();
        var unrelatedPlan = CreateSportPlan();
        var member = await _memberRepository.GetByActiveExternalConnectionAsync(
            ExternalProvider.Strava.Id,
            ExternalConnectionType.ActivitiesSync.Id,
            "42",
            CancellationToken.None);
        Assert.NotNull(member);
        member.SubscribeToPlan(inactiveLinkedPlan.Id, true, 1, true, _clock);
        _planRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([eligiblePlan, inactiveLinkedPlan, unrelatedPlan]);
        var occurredAt = Instant.FromUtc(2026, 1, 10, 8, 0);
        _adapter.FetchActivityAsync("42", "activity-1", Arg.Any<CancellationToken>())
            .Returns(new ActivitySyncFetchResultDto(
                ActivitySyncOutcome.Success,
                [
                    new AdapterActivityDto("other-provider", "GITHUB", "RUN", occurredAt, 1000),
                    new AdapterActivityDto("wrong-sport", "STRAVA", "WALK", occurredAt, 1000),
                    new AdapterActivityDto("missing-distance", "STRAVA", "RUN", occurredAt, null),
                    new AdapterActivityDto("future", "STRAVA", "RUN", Now.Plus(Duration.FromMinutes(1)), 1000),
                    new AdapterActivityDto("activity-1", "strava", "run", occurredAt, 1000),
                    new AdapterActivityDto("activity-1", "STRAVA", "RUN", occurredAt, 1000)
                ],
                WatermarkCandidate: Now));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            CreateCommand(externalActivityId: "activity-1", trigger: "webhook"),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.LogsCreated);
        Assert.Single(eligiblePlan.ActivityLogs);
        Assert.Equal(1f, eligiblePlan.ActivityLogs[0].Value);
        await _planRepository.Received(1).UpdateAsync(eligiblePlan, Arg.Any<CancellationToken>());
        await _planRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _adapter.Received(1).MarkSucceededAsync(
            "42", "webhook", null, 1, Arg.Any<CancellationToken>());
    }

    private ProcessExternalActivitySyncCommandHandler CreateHandler(
        IEnumerable<IActivitySyncAdapter>? adapters = null) =>
        new(
            _memberRepository,
            _planRepository,
            adapters ?? [_adapter],
            _clock);

    private (Member Member, Plan Plan) ConfigureEligibleRun()
    {
        var member = CreateMember();
        var plan = CreateSportPlan();
        plan.Activate(member.Id, _clock);
        member.SubscribeToPlan(plan.Id, true, 0, true, _clock);
        _memberRepository.GetByActiveExternalConnectionAsync(
                ExternalProvider.Strava.Id,
                ExternalConnectionType.ActivitiesSync.Id,
                "42",
                Arg.Any<CancellationToken>())
            .Returns(member);
        _planRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([plan]);
        return (member, plan);
    }

    private Member CreateMember() =>
        Member.Create("member", "Test", "", "Member", "", "UTC", _clock);

    private Plan CreateSportPlan(
        Instant? from = null,
        Instant? to = null) =>
        Plan.CreateSportPlan(
            "Running plan",
            "km",
            100,
            from ?? Instant.FromUtc(2026, 1, 1, 0, 0),
            to ?? Instant.FromUtc(2026, 1, 31, 23, 59),
            "2026-01-01",
            "2026-01-31",
            "UTC",
            true,
            new SportPlanDetails("km", [PlanthorSportType.Run.Id]),
            _clock,
            Guid.NewGuid());

    private static ProcessExternalActivitySyncCommand CreateCommand(
        string? externalActivityId = null,
        string trigger = "initial") =>
        new(new ExternalActivitySyncJobRequest(
            ExternalProvider.Strava.Id,
            "42",
            trigger,
            Guid.NewGuid().ToString("N"),
            externalActivityId));

    private sealed class TestClock(Instant current) : IClock
    {
        public Instant GetCurrentInstant() => current;
    }
}
