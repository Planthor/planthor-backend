using System;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava.Client;
using Adapters.Strava.EventHandlers;
using Domain.Members;
using Domain.Members.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NodaTime;

namespace UnitTests.Adapters.Strava.EventHandlers;

public class StravaConnectionRevokedEventHandlerTests
{
    private readonly IStravaApiClient _stravaClientMock;
    private readonly IMemberRepository _memberRepositoryMock;
    private readonly IServiceProvider _serviceProvider;
    private readonly StravaConnectionRevokedEventHandler _handler;
    private readonly IClock _clock;

    public StravaConnectionRevokedEventHandlerTests()
    {
        _stravaClientMock = Substitute.For<IStravaApiClient>();
        _memberRepositoryMock = Substitute.For<IMemberRepository>();
        _clock = SystemClock.Instance;

        var services = new ServiceCollection();
        services.AddSingleton(_memberRepositoryMock);
        _serviceProvider = services.BuildServiceProvider();

        _handler = new StravaConnectionRevokedEventHandler(
            _stravaClientMock,
            _serviceProvider,
            NullLogger<StravaConnectionRevokedEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenEventIsForStravaAndMemberExists_CallsDeauthorize()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var identifyName = "test-user";
        var member = Member.Create(identifyName, "John", null, "Doe", "Desc", "UTC", _clock);

        var domainEvent = new ExternalConnectionRevokedEvent(
            memberId,
            Guid.NewGuid(),
            ExternalProvider.Strava,
            ExternalConnectionType.ActivitiesSync,
            _clock,
            "test"
        );

        _memberRepositoryMock.GetByIdAsync(memberId, Arg.Any<CancellationToken>())
            .Returns(member);

        _stravaClientMock.DeauthorizeAsync(identifyName, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await _stravaClientMock.Received(1).DeauthorizeAsync(identifyName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEventIsNotForStrava_DoesNothing()
    {
        // Arrange
        var domainEvent = new ExternalConnectionRevokedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExternalProvider.Keycloak, // Not Strava
            ExternalConnectionType.ActivitiesSync,
            _clock,
            "test"
        );

        // Act
        await _handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await _memberRepositoryMock.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _stravaClientMock.DidNotReceive().DeauthorizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
