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
}
