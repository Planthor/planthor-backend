using System;
using System.Net.Http;
using Adapters.Strava.Configuration;
using Adapters.Strava.Coordinator;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Testing;

namespace Adapters.Tests.Strava;

public sealed class StravaRateLimitCoordinatorTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 9, 3, 10, 7, 30);

    [Fact]
    public void Observe_WithNullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        var coordinator = CreateCoordinator();

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => coordinator.Observe(null!));

        // Assert
        Assert.Equal("response", exception.ParamName);
    }

    [Fact]
    public void Observe_WithoutHeaders_PreservesDefaultsAndAllowsHistoricalWork()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        using var response = new HttpResponseMessage();

        // Act
        coordinator.Observe(response);

        // Assert
        Assert.Null(coordinator.HistoricalDeferral);
        Assert.Equal(Instant.FromUtc(2026, 9, 3, 10, 15, 5), coordinator.RetryAt);
    }

    [Fact]
    public void Observe_WithMalformedReadHeaders_UsesValidGenericHeaders()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("X-ReadRateLimit-Usage", ["invalid", "1,invalid", "invalid,2"]);
        response.Headers.TryAddWithoutValidation("X-RateLimit-Usage", "160,1000");
        response.Headers.TryAddWithoutValidation("X-ReadRateLimit-Limit", "200");
        response.Headers.TryAddWithoutValidation("X-RateLimit-Limit", "200,2000");

        // Act
        coordinator.Observe(response);

        // Assert
        Assert.Equal(Instant.FromUtc(2026, 9, 3, 10, 15, 5), coordinator.HistoricalDeferral);
        Assert.Equal(Instant.FromUtc(2026, 9, 3, 10, 15, 5), coordinator.RetryAt);
    }

    [Fact]
    public void Observe_WithDailyQuotaReached_DefersUntilNextUtcMidnight()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("X-ReadRateLimit-Usage", "1,2000");
        response.Headers.TryAddWithoutValidation("X-ReadRateLimit-Limit", "200,2000");

        // Act
        coordinator.Observe(response);

        // Assert
        Assert.Equal(Instant.FromUtc(2026, 9, 4, 0, 0, 5), coordinator.HistoricalDeferral);
        Assert.Equal(Instant.FromUtc(2026, 9, 4, 0, 0, 5), coordinator.RetryAt);
    }

    [Theory]
    [InlineData(-10, 199, false)]
    [InlineData(100, 19, false)]
    [InlineData(100, 20, true)]
    public void HistoricalDeferral_WithOutOfRangeHeadroom_ClampsConfiguredPercentage(
        int headroomPercentage,
        int shortWindowUsage,
        bool expectedDeferral)
    {
        // Arrange
        var coordinator = CreateCoordinator(headroomPercentage);
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation(
            "X-ReadRateLimit-Usage",
            $"{shortWindowUsage},0");
        response.Headers.TryAddWithoutValidation("X-ReadRateLimit-Limit", "200,2000");

        // Act
        coordinator.Observe(response);

        // Assert
        Assert.Equal(expectedDeferral, coordinator.HistoricalDeferral is not null);
    }

    private static StravaRateLimitCoordinator CreateCoordinator(int headroomPercentage = 20) =>
        new(
            new FakeClock(Now),
            Options.Create(new StravaOptions
            {
                BaseUrl = new Uri("https://www.strava.test"),
                HistoricalQuotaHeadroomPercentage = headroomPercentage
            }));
}
