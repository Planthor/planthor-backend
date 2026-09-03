using System;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava;
using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
using Adapters.Strava.EventHandlers;
using Adapters.Strava.Persistence;
using Application.Shared;
using Domain.Members;
using Domain.Members.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NodaTime;
using NodaTime.Testing;
using NSubstitute;

namespace Adapters.Tests.Strava;

public sealed class StravaConnectionEventHandlerTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 9, 3, 10, 0);
    private readonly IStravaApiClient _client = Substitute.For<IStravaApiClient>();
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly StravaAdapterDatabase _tokenDatabase;
    private readonly StravaActivitySyncAdapter _adapter;
    private readonly FakeClock _clock = new(Now);

    public StravaConnectionEventHandlerTests()
    {
        var mongoClient = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<StravaTokenDocument>>();
        mongoClient.GetDatabase(Arg.Any<string>()).Returns(database);
        database.GetCollection<StravaTokenDocument>(Arg.Any<string>()).Returns(collection);
        _tokenDatabase = Substitute.For<StravaAdapterDatabase>(mongoClient);
        _adapter = new StravaActivitySyncAdapter(_client, _tokenDatabase, _clock);
    }

    [Fact]
    public void EstablishedConstructor_WithNullDependencies_ThrowsArgumentNullExceptions()
    {
        // Arrange
        var options = Options.Create(new StravaOptions
        {
            BaseUrl = new Uri("https://www.strava.test")
        });

        // Act
        var adapterException = Assert.Throws<ArgumentNullException>(() =>
            new StravaConnectionEstablishedEventHandler(null!, _backgroundJobClient, options));
        var backgroundException = Assert.Throws<ArgumentNullException>(() =>
            new StravaConnectionEstablishedEventHandler(_adapter, null!, options));
        var optionsException = Assert.Throws<ArgumentNullException>(() =>
            new StravaConnectionEstablishedEventHandler(_adapter, _backgroundJobClient, null!));

        // Assert
        Assert.Equal("activitySyncAdapter", adapterException.ParamName);
        Assert.Equal("backgroundJobClient", backgroundException.ParamName);
        Assert.Equal("options", optionsException.ParamName);
    }

    [Fact]
    public async Task EstablishedHandleAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateEstablishedHandler(automaticSyncEnabled: true);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!, CancellationToken.None));

        // Assert
        Assert.Equal("domainEvent", exception.ParamName);
    }

    [Theory]
    [InlineData(false, "STRAVA", "ACTIVITIES_SYNC")]
    [InlineData(true, "GITHUB", "ACTIVITIES_SYNC")]
    [InlineData(true, "STRAVA", "IDENTITY")]
    public async Task EstablishedHandleAsync_WhenAutomaticSyncDoesNotApply_ReturnsWithoutScheduling(
        bool automaticSyncEnabled,
        string providerId,
        string connectionTypeId)
    {
        // Arrange
        var handler = CreateEstablishedHandler(automaticSyncEnabled);
        var domainEvent = CreateEstablishedEvent(
            ExternalProvider.FromId(providerId),
            ExternalConnectionType.FromId(connectionTypeId));

        // Act
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await _backgroundJobClient.DidNotReceiveWithAnyArgs()
            .EnqueueExternalActivitySyncAsync(default!, default);
    }

    [Fact]
    public async Task EstablishedHandleAsync_WhenStravaActivityConnectionEstablished_QueuesInitialSync()
    {
        // Arrange
        var token = new StravaTokenDocument { Id = "member-1", AthleteId = 42 };
        _tokenDatabase.GetByAthleteIdAsync(42, Arg.Any<CancellationToken>()).Returns(token);
        var handler = CreateEstablishedHandler(automaticSyncEnabled: true);
        var domainEvent = CreateEstablishedEvent(
            ExternalProvider.Strava,
            ExternalConnectionType.ActivitiesSync);

        // Act
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        Assert.Equal("queued", token.SyncState);
        Assert.Equal("queued", token.InitialSyncState);
        await _tokenDatabase.Received(1).UpsertAsync(token, Arg.Any<CancellationToken>());
        await _backgroundJobClient.Received(1).EnqueueExternalActivitySyncAsync(
            Arg.Is<ExternalActivitySyncJobRequest>(request =>
                request != null &&
                request.ProviderId == "STRAVA" &&
                request.ExternalUserId == "42" &&
                request.Trigger == "initial" &&
                request.IdempotencyKey == $"initial:{domainEvent.ExternalConnectionId}"),
            CancellationToken.None);
    }

    [Fact]
    public void RevokedConstructor_WithNullDependencies_ThrowsArgumentNullExceptions()
    {
        // Arrange
        var logger = NullLogger<StravaConnectionRevokedEventHandler>.Instance;

        // Act
        var clientException = Assert.Throws<ArgumentNullException>(() =>
            new StravaConnectionRevokedEventHandler(
                null!, _tokenDatabase, _adapter, _backgroundJobClient, logger));
        var databaseException = Assert.Throws<ArgumentNullException>(() =>
            new StravaConnectionRevokedEventHandler(
                _client, null!, _adapter, _backgroundJobClient, logger));
        var adapterException = Assert.Throws<ArgumentNullException>(() =>
            new StravaConnectionRevokedEventHandler(
                _client, _tokenDatabase, null!, _backgroundJobClient, logger));
        var backgroundException = Assert.Throws<ArgumentNullException>(() =>
            new StravaConnectionRevokedEventHandler(
                _client, _tokenDatabase, _adapter, null!, logger));

        // Assert
        Assert.Equal("stravaClient", clientException.ParamName);
        Assert.Equal("tokenDatabase", databaseException.ParamName);
        Assert.Equal("activitySyncAdapter", adapterException.ParamName);
        Assert.Equal("backgroundJobClient", backgroundException.ParamName);
    }

    [Fact]
    public async Task RevokedHandleAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateRevokedHandler();

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!, CancellationToken.None));

        // Assert
        Assert.Equal("domainEvent", exception.ParamName);
    }

    [Theory]
    [InlineData("GITHUB", "ACTIVITIES_SYNC")]
    [InlineData("STRAVA", "IDENTITY")]
    public async Task RevokedHandleAsync_WhenEventIsNotStravaActivityConnection_ReturnsWithoutCleanup(
        string providerId,
        string connectionTypeId)
    {
        // Arrange
        var handler = CreateRevokedHandler();
        var domainEvent = CreateRevokedEvent(
            ExternalProvider.FromId(providerId),
            ExternalConnectionType.FromId(connectionTypeId),
            "42");

        // Act
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await _backgroundJobClient.DidNotReceiveWithAnyArgs()
            .CancelExternalActivitySyncAsync(default!, default!, default);
        await _tokenDatabase.DidNotReceiveWithAnyArgs()
            .DeleteByAthleteIdAsync(default, default);
    }

    [Fact]
    public async Task RevokedHandleAsync_WithStoredToken_DeauthorizesAndDeletesOperationalData()
    {
        // Arrange
        var token = new StravaTokenDocument { Id = "member-1", AthleteId = 42 };
        _tokenDatabase.GetByAthleteIdAsync(42, Arg.Any<CancellationToken>()).Returns(token);
        var handler = CreateRevokedHandler();
        var domainEvent = CreateRevokedEvent(
            ExternalProvider.Strava,
            ExternalConnectionType.ActivitiesSync,
            "42");

        // Act
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await _backgroundJobClient.Received(1).CancelExternalActivitySyncAsync(
            "STRAVA", "42", CancellationToken.None);
        await _client.Received(1).DeauthorizeAsync("member-1", CancellationToken.None);
        await _tokenDatabase.Received(1).DeleteByAthleteIdAsync(42, CancellationToken.None);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("43")]
    public async Task RevokedHandleAsync_WithoutStoredToken_SkipsUpstreamDeauthorization(string externalUserId)
    {
        // Arrange
        _tokenDatabase.GetByAthleteIdAsync(43, Arg.Any<CancellationToken>())
            .Returns((StravaTokenDocument?)null);
        var handler = CreateRevokedHandler();
        var domainEvent = CreateRevokedEvent(
            ExternalProvider.Strava,
            ExternalConnectionType.ActivitiesSync,
            externalUserId);

        // Act
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await _client.DidNotReceiveWithAnyArgs().DeauthorizeAsync(default!, default);
        if (externalUserId == "43")
        {
            await _tokenDatabase.Received(1).DeleteByAthleteIdAsync(43, CancellationToken.None);
        }
    }

    [Fact]
    public async Task RevokedHandleAsync_WhenUpstreamCleanupFails_StillDeletesOperationalData()
    {
        // Arrange
        _backgroundJobClient.CancelExternalActivitySyncAsync(
                "STRAVA",
                "42",
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Scheduler unavailable."));
        var handler = CreateRevokedHandler();
        var domainEvent = CreateRevokedEvent(
            ExternalProvider.Strava,
            ExternalConnectionType.ActivitiesSync,
            "42");

        // Act
        var exception = await Record.ExceptionAsync(() =>
            handler.HandleAsync(domainEvent, CancellationToken.None));

        // Assert
        Assert.Null(exception);
        await _tokenDatabase.Received(1).DeleteByAthleteIdAsync(42, CancellationToken.None);
    }

    private StravaConnectionEstablishedEventHandler CreateEstablishedHandler(bool automaticSyncEnabled) =>
        new(
            _adapter,
            _backgroundJobClient,
            Options.Create(new StravaOptions
            {
                BaseUrl = new Uri("https://www.strava.test"),
                AutomaticSyncEnabled = automaticSyncEnabled
            }));

    private StravaConnectionRevokedEventHandler CreateRevokedHandler() =>
        new(
            _client,
            _tokenDatabase,
            _adapter,
            _backgroundJobClient,
            NullLogger<StravaConnectionRevokedEventHandler>.Instance);

    private ExternalConnectionEstablishedEvent CreateEstablishedEvent(
        ExternalProvider provider,
        ExternalConnectionType connectionType) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            provider,
            connectionType,
            "42",
            _clock,
            "test");

    private ExternalConnectionRevokedEvent CreateRevokedEvent(
        ExternalProvider provider,
        ExternalConnectionType connectionType,
        string externalUserId) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            provider,
            connectionType,
            externalUserId,
            _clock,
            "test");
}
