using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava;
using Adapters.Strava.Client;
using Adapters.Strava.Persistence;
using Application.Dtos;
using Application.Shared;
using MongoDB.Driver;
using NodaTime;
using NodaTime.Testing;
using NSubstitute;

namespace Adapters.Tests.Strava;

public class StravaActivitySyncAdapterTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 9, 2, 0, 0);
    private readonly IStravaApiClient _client = Substitute.For<IStravaApiClient>();
    private readonly StravaAdapterDatabase _tokenDatabase;
    private readonly StravaActivitySyncAdapter _adapter;

    public StravaActivitySyncAdapterTests()
    {
        var mongoClient = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<StravaTokenDocument>>();
        mongoClient.GetDatabase(Arg.Any<string>()).Returns(database);
        database.GetCollection<StravaTokenDocument>(Arg.Any<string>()).Returns(collection);
        _tokenDatabase = Substitute.For<StravaAdapterDatabase>(mongoClient);
        _adapter = new StravaActivitySyncAdapter(
            _client,
            _tokenDatabase,
            new FakeClock(Now));
    }

    [Fact]
    public async Task FetchActivitiesAsync_MissingAthleteToken_ReturnsAuthorizationRequired()
    {
        // Arrange
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>())
            .Returns((StravaTokenDocument?)null);

        // Act
        var result = await _adapter.FetchActivitiesAsync(
            "123",
            Now.Minus(Duration.FromDays(30)),
            Now,
            CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.AuthorizationRequired, result.Outcome);
        Assert.Empty(result.Activities);
    }

    [Fact]
    public async Task FetchActivitiesAsync_Success_NormalizesAndDoesNotAdvanceWatermark()
    {
        // Arrange
        var rangeStart = Now.Minus(Duration.FromDays(30));
        var token = new StravaTokenDocument
        {
            Id = "member-1",
            AthleteId = 123,
            LastSyncEpoch = rangeStart.ToUnixTimeSeconds()
        };
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);
        _client.GetAthleteActivitiesPageAsync(
                token.Id,
                token.LastSyncEpoch.Value,
                Now.ToUnixTimeSeconds() + 1,
                1,
                200,
                Arg.Any<CancellationToken>())
            .Returns(new StravaApiResult<IReadOnlyList<StravaActivityResponse>>(
                StravaApiOutcome.Success,
                [
                    new StravaActivityResponse
                    {
                        Id = 456,
                        SportType = "TrailRun",
                        Distance = 5000,
                        StartDate = Now.Minus(Duration.FromDays(1)).ToDateTimeOffset()
                    },
                    new StravaActivityResponse
                    {
                        Id = 789,
                        SportType = "IceSkate",
                        Distance = 1000,
                        StartDate = Now.ToDateTimeOffset()
                    }
                ]));

        // Act
        var result = await _adapter.FetchActivitiesAsync("123", rangeStart, Now, CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.Success, result.Outcome);
        var activity = Assert.Single(result.Activities);
        Assert.Equal("456", activity.ExternalActivityId);
        Assert.Equal("RUN", activity.CanonicalSportTypeId);
        Assert.Equal(5000d, activity.DistanceMeters);
        Assert.Equal(Now, result.WatermarkCandidate);
        await _tokenDatabase.DidNotReceive().UpsertAsync(
            Arg.Any<StravaTokenDocument>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchActivityAsync_RateLimited_MapsTypedDeferral()
    {
        // Arrange
        var retryAt = Now.Plus(Duration.FromMinutes(15));
        var token = new StravaTokenDocument { Id = "member-1", AthleteId = 123 };
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);
        _client.GetActivityAsync(token.Id, "456", Arg.Any<CancellationToken>())
            .Returns(new StravaApiResult<StravaActivityResponse>(
                StravaApiOutcome.RateLimited,
                RetryAt: retryAt,
                ErrorCode: "strava_rate_limited"));

        // Act
        var result = await _adapter.FetchActivityAsync("123", "456", CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.RateLimited, result.Outcome);
        Assert.Equal(retryAt, result.RetryAt);
        Assert.Equal("strava_rate_limited", result.ErrorCode);
    }

    [Fact]
    public async Task MarkSucceededAsync_HistoricalRun_AdvancesWatermarkAfterCommitBoundary()
    {
        // Arrange
        var token = new StravaTokenDocument { Id = "member-1", AthleteId = 123 };
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);

        // Act
        await _adapter.MarkSucceededAsync("123", "initial", Now, 2, CancellationToken.None);

        // Assert
        await _tokenDatabase.Received(1).UpsertAsync(
            Arg.Is<StravaTokenDocument>(document => document != null &&
                document.LastSyncEpoch == Now.ToUnixTimeSeconds() &&
                document.InitialSyncState == "succeeded" &&
                document.ActivityLogsCreated == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullExceptions()
    {
        // Arrange
        var clock = new FakeClock(Now);

        // Act
        var clientException = Assert.Throws<ArgumentNullException>(() =>
            new StravaActivitySyncAdapter(null!, _tokenDatabase, clock));
        var databaseException = Assert.Throws<ArgumentNullException>(() =>
            new StravaActivitySyncAdapter(_client, null!, clock));
        var clockException = Assert.Throws<ArgumentNullException>(() =>
            new StravaActivitySyncAdapter(_client, _tokenDatabase, null!));

        // Assert
        Assert.Equal("client", clientException.ParamName);
        Assert.Equal("tokenDb", databaseException.ParamName);
        Assert.Equal("clock", clockException.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task FetchMethods_WithMissingRequiredArguments_ThrowArgumentExceptions(string? missingValue)
    {
        // Arrange
        const string validValue = "123";

        // Act
        var activitiesException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _adapter.FetchActivitiesAsync(missingValue!, Now, Now, CancellationToken.None));
        var activityUserException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _adapter.FetchActivityAsync(missingValue!, validValue, CancellationToken.None));
        var activityIdException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _adapter.FetchActivityAsync(validValue, missingValue!, CancellationToken.None));

        // Assert
        Assert.NotNull(activitiesException);
        Assert.NotNull(activityUserException);
        Assert.NotNull(activityIdException);
    }

    [Fact]
    public async Task FetchActivitiesAsync_WithInvalidAthleteId_ReturnsAuthorizationRequired()
    {
        // Arrange
        const string externalUserId = "not-a-number";

        // Act
        var result = await _adapter.FetchActivitiesAsync(
            externalUserId,
            Now.Minus(Duration.FromDays(1)),
            Now,
            CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.AuthorizationRequired, result.Outcome);
        Assert.Equal("strava_token_missing", result.ErrorCode);
    }

    [Fact]
    public async Task FetchActivitiesAsync_WithCompletedRange_ReturnsWatermarkWithoutCallingProvider()
    {
        // Arrange
        var rangeEnd = Now.Minus(Duration.FromDays(1));
        var token = new StravaTokenDocument
        {
            Id = "member-1",
            AthleteId = 123,
            LastSyncEpoch = Now.ToUnixTimeSeconds()
        };
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);

        // Act
        var result = await _adapter.FetchActivitiesAsync(
            "123",
            Now.Minus(Duration.FromDays(30)),
            rangeEnd,
            CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.Success, result.Outcome);
        Assert.Equal(rangeEnd, result.WatermarkCandidate);
        await _client.DidNotReceiveWithAnyArgs()
            .GetAthleteActivitiesPageAsync(default!, default, default, default, default, default);
    }

    [Fact]
    public async Task FetchActivitiesAsync_WithMultiplePages_MapsFallbackTypeAndSkipsUnsupportedSport()
    {
        // Arrange
        var token = new StravaTokenDocument { Id = "member-1", AthleteId = 123 };
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);
        var fullPage = Enumerable.Range(1, 200)
            .Select(id => new StravaActivityResponse
            {
                Id = id,
                SportType = "Run",
                Distance = 1000,
                StartDate = Now.ToDateTimeOffset()
            })
            .ToArray();
        _client.GetAthleteActivitiesPageAsync(
                token.Id,
                Arg.Any<long>(),
                Arg.Any<long>(),
                1,
                200,
                Arg.Any<CancellationToken>())
            .Returns(new StravaApiResult<IReadOnlyList<StravaActivityResponse>>(
                StravaApiOutcome.Success,
                fullPage));
        _client.GetAthleteActivitiesPageAsync(
                token.Id,
                Arg.Any<long>(),
                Arg.Any<long>(),
                2,
                200,
                Arg.Any<CancellationToken>())
            .Returns(new StravaApiResult<IReadOnlyList<StravaActivityResponse>>(
                StravaApiOutcome.Success,
                [
                    new StravaActivityResponse
                    {
                        Id = 201,
                        SportType = "",
                        Type = "Ride",
                        Distance = 2000,
                        StartDate = Now.ToDateTimeOffset()
                    },
                    new StravaActivityResponse
                    {
                        Id = 202,
                        SportType = "AlpineSki",
                        Distance = 3000,
                        StartDate = Now.ToDateTimeOffset()
                    }
                ]));

        // Act
        var result = await _adapter.FetchActivitiesAsync(
            "123",
            Now.Minus(Duration.FromDays(30)),
            Now,
            CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.Success, result.Outcome);
        Assert.Equal(201, result.Activities.Count);
        Assert.Equal("RIDE", result.Activities[^1].CanonicalSportTypeId);
    }

    [Theory]
    [InlineData("NOT_FOUND", "N")]
    [InlineData("AUTHORIZATION_REQUIRED", "A")]
    [InlineData("RATE_LIMITED", "R")]
    [InlineData("TRANSIENT_FAILURE", "T")]
    public async Task FetchActivityAsync_WithProviderFailure_MapsProviderNeutralOutcome(
        string apiOutcomeId,
        string expectedOutcomeId)
    {
        // Arrange
        var token = new StravaTokenDocument { Id = "member-1", AthleteId = 123 };
        var retryAt = Now.Plus(Duration.FromMinutes(5));
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);
        _client.GetActivityAsync(token.Id, "456", Arg.Any<CancellationToken>())
            .Returns(new StravaApiResult<StravaActivityResponse>(
                StravaApiOutcome.FromId(apiOutcomeId),
                RetryAt: retryAt,
                ErrorCode: "provider_error"));

        // Act
        var result = await _adapter.FetchActivityAsync("123", "456", CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.FromId(expectedOutcomeId), result.Outcome);
        Assert.Equal(retryAt, result.RetryAt);
        Assert.Equal("provider_error", result.ErrorCode);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task FetchActivityAsync_WithSuccess_MapsOnlySupportedNonNullActivity(
        bool hasValue,
        bool isSupported,
        int expectedCount)
    {
        // Arrange
        var token = new StravaTokenDocument { Id = "member-1", AthleteId = 123 };
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);
        var value = hasValue
            ? new StravaActivityResponse
            {
                Id = 456,
                SportType = isSupported ? "Run" : "AlpineSki",
                Distance = 1000,
                StartDate = Now.ToDateTimeOffset()
            }
            : null;
        _client.GetActivityAsync(token.Id, "456", Arg.Any<CancellationToken>())
            .Returns(new StravaApiResult<StravaActivityResponse>(StravaApiOutcome.Success, value));

        // Act
        var result = await _adapter.FetchActivityAsync("123", "456", CancellationToken.None);

        // Assert
        Assert.Equal(ActivitySyncOutcome.Success, result.Outcome);
        Assert.Equal(expectedCount, result.Activities.Count);
    }

    [Fact]
    public async Task StatusMethods_WithInitialAndManualTriggers_PersistAllStateTransitions()
    {
        // Arrange
        var queued = new StravaTokenDocument { Id = "queued", AthleteId = 1 };
        var running = new StravaTokenDocument { Id = "running", AthleteId = 2 };
        var succeeded = new StravaTokenDocument
        {
            Id = "succeeded",
            AthleteId = 3,
            LastSyncEpoch = Now.ToUnixTimeSeconds()
        };
        var manualSuccess = new StravaTokenDocument { Id = "manual-success", AthleteId = 4 };
        var deferred = new StravaTokenDocument { Id = "deferred", AthleteId = 5 };
        var failed = new StravaTokenDocument { Id = "failed", AthleteId = 6 };
        _tokenDatabase.GetByAthleteIdAsync(1, Arg.Any<CancellationToken>()).Returns(queued);
        _tokenDatabase.GetByAthleteIdAsync(2, Arg.Any<CancellationToken>()).Returns(running);
        _tokenDatabase.GetByAthleteIdAsync(3, Arg.Any<CancellationToken>()).Returns(succeeded);
        _tokenDatabase.GetByAthleteIdAsync(4, Arg.Any<CancellationToken>()).Returns(manualSuccess);
        _tokenDatabase.GetByAthleteIdAsync(5, Arg.Any<CancellationToken>()).Returns(deferred);
        _tokenDatabase.GetByAthleteIdAsync(6, Arg.Any<CancellationToken>()).Returns(failed);
        var retryAt = Now.Plus(Duration.FromMinutes(5));

        // Act
        await _adapter.MarkQueuedAsync("1", ExternalActivitySyncTrigger.Initial, CancellationToken.None);
        await _adapter.MarkRunningAsync("2", ExternalActivitySyncTrigger.Manual, CancellationToken.None);
        await _adapter.MarkSucceededAsync(
            "3",
            ExternalActivitySyncTrigger.Initial,
            Now.Minus(Duration.FromMinutes(1)),
            2,
            CancellationToken.None);
        await _adapter.MarkSucceededAsync(
            "4",
            ExternalActivitySyncTrigger.Manual,
            null,
            1,
            CancellationToken.None);
        await _adapter.MarkDeferredAsync(
            "5",
            ExternalActivitySyncTrigger.Initial,
            retryAt,
            "rate_limited",
            CancellationToken.None);
        await _adapter.MarkFailedAsync(
            "6",
            ExternalActivitySyncTrigger.Manual,
            "authorization_required",
            CancellationToken.None);

        // Assert
        Assert.Equal("queued", queued.InitialSyncState);
        Assert.Equal("running", running.SyncState);
        Assert.Equal("manual", running.LastSyncTrigger);
        Assert.Equal(Now.ToUnixTimeSeconds(), succeeded.LastSyncEpoch);
        Assert.Equal("succeeded", succeeded.InitialSyncState);
        Assert.Equal(2, succeeded.ActivityLogsCreated);
        Assert.Equal("succeeded", manualSuccess.SyncState);
        Assert.Equal("deferred", deferred.InitialSyncState);
        Assert.Equal(retryAt.ToDateTimeOffset(), deferred.NextSyncAttemptAtUtc);
        Assert.Equal("failed", failed.SyncState);
        Assert.Equal("not_started", failed.InitialSyncState);
        await _tokenDatabase.Received(6).UpsertAsync(
            Arg.Any<StravaTokenDocument>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StatusMethods_WithMissingAndExistingTokens_ReturnExpectedStatus()
    {
        // Arrange
        var token = new StravaTokenDocument
        {
            Id = "member-1",
            AthleteId = 123,
            InitialSyncState = "succeeded",
            SyncState = "idle",
            LastSyncTrigger = "initial",
            LastSyncStartedAtUtc = Now.ToDateTimeOffset(),
            LastSuccessfulSyncAtUtc = Now.ToDateTimeOffset(),
            NextSyncAttemptAtUtc = Now.Plus(Duration.FromMinutes(5)).ToDateTimeOffset(),
            SyncErrorCode = "none"
        };
        _tokenDatabase.GetByAthleteIdAsync(123, Arg.Any<CancellationToken>()).Returns(token);
        _tokenDatabase.GetByAthleteIdAsync(999, Arg.Any<CancellationToken>())
            .Returns((StravaTokenDocument?)null);

        // Act
        await _adapter.MarkQueuedAsync("999", "manual", CancellationToken.None);
        var missingStatus = await _adapter.GetSyncStatusAsync("invalid", CancellationToken.None);
        var status = await _adapter.GetSyncStatusAsync("123", CancellationToken.None);
        await _adapter.DeleteOperationalDataAsync("123", CancellationToken.None);
        await _adapter.DeleteOperationalDataAsync("invalid", CancellationToken.None);

        // Assert
        Assert.Null(missingStatus);
        Assert.NotNull(status);
        Assert.Equal("STRAVA", status.ProviderId);
        Assert.Equal("succeeded", status.InitialSyncState);
        Assert.Equal("idle", status.State);
        Assert.Equal("none", status.ErrorCode);
        await _tokenDatabase.Received(1).DeleteByAthleteIdAsync(123, Arg.Any<CancellationToken>());
        await _tokenDatabase.DidNotReceive().DeleteByAthleteIdAsync(0, Arg.Any<CancellationToken>());
    }
}
