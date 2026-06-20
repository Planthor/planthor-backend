using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava;
using Adapters.Strava.Client;
using Moq;
using NodaTime;

namespace Adapters.Tests.Strava;

public class StravaActivitySyncAdapterTests
{
    private readonly Mock<StravaApiClient> _mockStravaApiClient;
    private readonly StravaActivitySyncAdapter _adapter;

    public StravaActivitySyncAdapterTests()
    {
        _mockStravaApiClient = new Mock<StravaApiClient>();
        _adapter = new StravaActivitySyncAdapter(_mockStravaApiClient.Object);
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
        var since = Instant.FromUtc(2026, 5, 1, 0, 0, 0);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _adapter.FetchActivitiesAsync(memberId, since, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<object>>(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchActivitiesAsync_SupportsIActivitySyncAdapterContract()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var since = Instant.FromUtc(2026, 4, 1, 0, 0, 0);

        // Act
        var result = await _adapter.FetchActivitiesAsync(memberId, since, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<object>>(result);
    }
}
