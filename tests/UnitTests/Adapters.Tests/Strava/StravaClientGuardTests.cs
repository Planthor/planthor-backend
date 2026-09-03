using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
using Adapters.Strava.Coordinator;
using Adapters.Strava.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NodaTime;
using NodaTime.Testing;
using NSubstitute;

namespace Adapters.Tests.Strava;

public sealed class StravaClientGuardTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 9, 3, 10, 0);
    private readonly HttpClient _httpClient = new(new RecordingHttpMessageHandler());
    private readonly StravaAdapterDatabase _tokenDatabase;
    private readonly IOptions<StravaOptions> _options = Options.Create(new StravaOptions
    {
        BaseUrl = new Uri("https://www.strava.test"),
        ClientId = "client",
        ClientSecret = "secret",
        HistoricalQuotaHeadroomPercentage = 20
    });
    private readonly FakeClock _clock = new(Now);

    public StravaClientGuardTests()
    {
        var mongoClient = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<StravaTokenDocument>>();
        mongoClient.GetDatabase(Arg.Any<string>()).Returns(database);
        database.GetCollection<StravaTokenDocument>(Arg.Any<string>()).Returns(collection);
        _tokenDatabase = Substitute.For<StravaAdapterDatabase>(mongoClient);
    }

    [Fact]
    public void StravaTokenClient_WithNullDependencies_ThrowsArgumentNullExceptions()
    {
        // Arrange
        var logger = NullLogger<StravaTokenClient>.Instance;

        // Act
        var httpException = Assert.Throws<ArgumentNullException>(() =>
            new StravaTokenClient(null!, _tokenDatabase, _options, logger, _clock));
        var databaseException = Assert.Throws<ArgumentNullException>(() =>
            new StravaTokenClient(_httpClient, null!, _options, logger, _clock));
        var clockException = Assert.Throws<ArgumentNullException>(() =>
            new StravaTokenClient(_httpClient, _tokenDatabase, _options, logger, null!));

        // Assert
        Assert.Equal("httpClient", httpException.ParamName);
        Assert.Equal("tokenDb", databaseException.ParamName);
        Assert.Equal("clock", clockException.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task StravaTokenClient_WithMissingArguments_ThrowsArgumentExceptions(string? missingValue)
    {
        // Arrange
        var client = CreateTokenClient();

        // Act
        var exchangeCodeException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            client.ExchangeCodeAsync(missingValue!, "member", CancellationToken.None));
        var exchangeMemberException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            client.ExchangeCodeAsync("code", missingValue!, CancellationToken.None));
        var refreshException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            client.RefreshTokenAsync(missingValue!, CancellationToken.None));
        var validTokenException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            client.GetValidTokenAsync(missingValue!, CancellationToken.None));
        var deauthorizeException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            client.DeauthorizeAsync(missingValue!, CancellationToken.None));

        // Assert
        Assert.NotNull(exchangeCodeException);
        Assert.NotNull(exchangeMemberException);
        Assert.NotNull(refreshException);
        Assert.NotNull(validTokenException);
        Assert.NotNull(deauthorizeException);
    }

    [Fact]
    public void StravaApiClient_WithNullDependencies_ThrowsArgumentNullExceptions()
    {
        // Arrange
        var tokenClient = CreateTokenClient();
        var coordinator = CreateCoordinator();
        var logger = NullLogger<StravaApiClient>.Instance;

        // Act
        var httpException = Assert.Throws<ArgumentNullException>(() =>
            new StravaApiClient(null!, tokenClient, _options, logger, _clock, coordinator));
        var tokenException = Assert.Throws<ArgumentNullException>(() =>
            new StravaApiClient(_httpClient, null!, _options, logger, _clock, coordinator));
        var clockException = Assert.Throws<ArgumentNullException>(() =>
            new StravaApiClient(_httpClient, tokenClient, _options, logger, null!, coordinator));
        var coordinatorException = Assert.Throws<ArgumentNullException>(() =>
            new StravaApiClient(_httpClient, tokenClient, _options, logger, _clock, null!));

        // Assert
        Assert.Equal("httpClient", httpException.ParamName);
        Assert.Equal("tokenClient", tokenException.ParamName);
        Assert.Equal("clock", clockException.ParamName);
        Assert.Equal("rateLimitCoordinator", coordinatorException.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetAthleteActivitiesPageAsync_WithMissingMember_ThrowsArgumentException(
        string? missingValue)
    {
        // Arrange
        var client = CreateApiClient(CreateCoordinator());

        // Act
        var exception = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            client.GetAthleteActivitiesPageAsync(
                missingValue!, 0, 1, 1, 30, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetAthleteActivitiesPageAsync_WhenHistoricalHeadroomReached_ReturnsDeferral()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("X-ReadRateLimit-Usage", "160,1000");
        response.Headers.TryAddWithoutValidation("X-ReadRateLimit-Limit", "200,2000");
        coordinator.Observe(response);
        var client = CreateApiClient(coordinator);

        // Act
        var result = await client.GetAthleteActivitiesPageAsync(
            "member", 0, 1, 1, 30, CancellationToken.None);

        // Assert
        Assert.Equal(StravaApiOutcome.RateLimited, result.Outcome);
        Assert.Equal("strava_rate_limit_headroom", result.ErrorCode);
        Assert.Equal(Instant.FromUtc(2026, 9, 3, 10, 15, 5), result.RetryAt);
    }

    private StravaTokenClient CreateTokenClient() =>
        new(
            _httpClient,
            _tokenDatabase,
            _options,
            NullLogger<StravaTokenClient>.Instance,
            _clock);

    private StravaApiClient CreateApiClient(StravaRateLimitCoordinator coordinator) =>
        new(
            _httpClient,
            CreateTokenClient(),
            _options,
            NullLogger<StravaApiClient>.Instance,
            _clock,
            coordinator);

    private StravaRateLimitCoordinator CreateCoordinator() =>
        new(_clock, _options);

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage());
    }
}
