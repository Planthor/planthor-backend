using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava;
using Adapters.Strava.Client;
using Adapters.Strava.Persistence;
using MongoDB.Driver;
using NodaTime;
using NSubstitute;

namespace Adapters.Tests.Strava;

public class StravaActivitySyncAdapterTests
{
    private readonly IStravaApiClient _mockStravaApiClient;
    private readonly StravaAdapterDatabase _mockTokenDb;
    private readonly StravaActivitySyncAdapter _adapter;

    public StravaActivitySyncAdapterTests()
    {
        _mockStravaApiClient = Substitute.For<IStravaApiClient>();
        
        var mockClient = Substitute.For<IMongoClient>();
        var mockDb = Substitute.For<IMongoDatabase>();
        var mockCollection = Substitute.For<IMongoCollection<StravaTokenDocument>>();
        mockClient.GetDatabase(Arg.Any<string>()).Returns(mockDb);
        mockDb.GetCollection<StravaTokenDocument>(Arg.Any<string>()).Returns(mockCollection);
        
        _mockTokenDb = Substitute.For<StravaAdapterDatabase>(mockClient);
        
        _adapter = new StravaActivitySyncAdapter(_mockStravaApiClient, _mockTokenDb);
    }

    [Fact]
    public void ProviderId_ReturnsStrava()
    {
        // Act
        var providerId = _adapter.ProviderId;

        // Assert
        Assert.Equal("STRAVA", providerId);
    }

    [Fact]
    public async Task FetchActivitiesAsync_WithValidMemberId_ReturnsEmptyCollection()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var identifyName = "test-user";
        var since = Instant.FromUtc(2026, 5, 1, 0, 0, 0);
        var cancellationToken = CancellationToken.None;

        _mockTokenDb.GetByIdentifyNameAsync(identifyName, cancellationToken).Returns(Task.FromResult<StravaTokenDocument?>(null));

        // Act
        var result = await _adapter.FetchActivitiesAsync(memberId, identifyName, since, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IReadOnlyList<object>>(result, exactMatch: false);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchActivitiesAsync_SupportsIActivitySyncAdapterContract()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var identifyName = "test-user";
        var since = Instant.FromUtc(2026, 4, 1, 0, 0, 0);

        _mockTokenDb.GetByIdentifyNameAsync(identifyName, Arg.Any<CancellationToken>()).Returns(Task.FromResult<StravaTokenDocument?>(null));

        // Act
        var result = await _adapter.FetchActivitiesAsync(memberId, identifyName, since, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IReadOnlyList<object>>(result, exactMatch: false);
    }

    [Fact]
    public async Task FetchActivitiesAsync_NullIdentifyName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _adapter.FetchActivitiesAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task FetchActivitiesAsync_WithExistingTokenAndNewActivities_ReturnsActivitiesAndUpdatesToken()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var identifyName = "test-user";
        var lastSyncEpoch = Instant.FromUtc(2026, 6, 1, 0, 0, 0).ToUnixTimeSeconds();
        var tokenDoc = new StravaTokenDocument { Id = identifyName, LastSyncEpoch = lastSyncEpoch };

        _mockTokenDb.GetByIdentifyNameAsync(identifyName, Arg.Any<CancellationToken>()).Returns(Task.FromResult<StravaTokenDocument?>(tokenDoc));

        var stravaActivities = new List<StravaActivityResponse>
        {
            new() { Id = 1, Name = "Morning Run", StartDate = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), SportType = "Run", Distance = 5000 },
            new() { Id = 2, Name = "", StartDate = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc), Type = "Ride", Distance = 10000 }
        };

        _mockStravaApiClient.GetAthleteActivitiesAsync(identifyName, lastSyncEpoch, 1, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StravaActivityResponse>>(stravaActivities));

        _mockStravaApiClient.GetAthleteActivitiesAsync(identifyName, lastSyncEpoch, 2, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StravaActivityResponse>>([]));

        // Act
        var result = await _adapter.FetchActivitiesAsync(memberId, identifyName, null, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Morning Run", result[0].Name);
        Assert.Equal("Strava Activity", result[1].Name);
        Assert.Equal("Run", result[0].ActivityType);
        Assert.Equal("Ride", result[1].ActivityType);

        await _mockTokenDb.Received(1).UpsertAsync(Arg.Is<StravaTokenDocument>(d => d.LastSyncEpoch > lastSyncEpoch), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchActivitiesAsync_SinceParamGreaterThanLastSync_UsesSinceParam()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var identifyName = "test-user";
        var lastSyncEpoch = Instant.FromUtc(2026, 6, 1, 0, 0, 0).ToUnixTimeSeconds();
        var tokenDoc = new StravaTokenDocument { Id = identifyName, LastSyncEpoch = lastSyncEpoch };
        var since = Instant.FromUtc(2026, 6, 5, 0, 0, 0); // Newer than LastSyncEpoch

        _mockTokenDb.GetByIdentifyNameAsync(identifyName, Arg.Any<CancellationToken>()).Returns(Task.FromResult<StravaTokenDocument?>(tokenDoc));

        _mockStravaApiClient.GetAthleteActivitiesAsync(identifyName, since.ToUnixTimeSeconds(), 1, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StravaActivityResponse>>([]));

        // Act
        var result = await _adapter.FetchActivitiesAsync(memberId, identifyName, since, CancellationToken.None);

        // Assert
        Assert.Empty(result);
        await _mockStravaApiClient.Received(1).GetAthleteActivitiesAsync(identifyName, since.ToUnixTimeSeconds(), 1, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchActivitiesAsync_PaginatedResults_FetchesAllPages()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var identifyName = "test-user";
        var lastSyncEpoch = Instant.FromUtc(2026, 6, 1, 0, 0, 0).ToUnixTimeSeconds();
        var tokenDoc = new StravaTokenDocument { Id = identifyName, LastSyncEpoch = lastSyncEpoch };

        _mockTokenDb.GetByIdentifyNameAsync(identifyName, Arg.Any<CancellationToken>()).Returns(Task.FromResult<StravaTokenDocument?>(tokenDoc));

        var page1 = new List<StravaActivityResponse>();
        for(int i = 0; i < 100; i++) 
        {
            page1.Add(new StravaActivityResponse { Id = i, StartDate = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc) });
        }

        var page2 = new List<StravaActivityResponse>
        {
            new() { Id = 101, StartDate = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc) }
        };

        _mockStravaApiClient.GetAthleteActivitiesAsync(identifyName, lastSyncEpoch, 1, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StravaActivityResponse>>(page1));
            
        _mockStravaApiClient.GetAthleteActivitiesAsync(identifyName, lastSyncEpoch, 2, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StravaActivityResponse>>(page2));

        // Act
        var result = await _adapter.FetchActivitiesAsync(memberId, identifyName, null, CancellationToken.None);

        // Assert
        Assert.Equal(101, result.Count);
        await _mockStravaApiClient.Received(1).GetAthleteActivitiesAsync(identifyName, lastSyncEpoch, 1, 100, Arg.Any<CancellationToken>());
        await _mockStravaApiClient.Received(1).GetAthleteActivitiesAsync(identifyName, lastSyncEpoch, 2, 100, Arg.Any<CancellationToken>());
    }
}
