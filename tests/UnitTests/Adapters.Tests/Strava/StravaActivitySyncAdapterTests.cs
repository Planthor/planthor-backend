using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava;
using Adapters.Strava.Client;
using Adapters.Strava.Persistence;
using Application.Dtos;
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
}
