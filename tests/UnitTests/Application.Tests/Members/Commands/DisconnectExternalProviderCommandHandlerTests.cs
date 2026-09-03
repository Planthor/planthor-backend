using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.DisconnectExternalProvider;
using Domain.Members;
using NodaTime;
using NSubstitute;

namespace Application.Tests.Members.Commands;

public class DisconnectExternalProviderCommandHandlerTests
{
    private readonly IMemberRepository _memberRepositoryMock;
    private readonly IClock _clockMock;
    private readonly DisconnectExternalProviderCommandHandler _handler;

    public DisconnectExternalProviderCommandHandlerTests()
    {
        _memberRepositoryMock = Substitute.For<IMemberRepository>();
        _clockMock = Substitute.For<IClock>();
        _clockMock.GetCurrentInstant().Returns(Instant.FromUtc(2025, 1, 1, 0, 0));
        _handler = new DisconnectExternalProviderCommandHandler(_memberRepositoryMock, _clockMock);
    }

    [Fact]
    public async Task Handle_WithValidRequest_RevokesConnectionAndUpdatesRepository()
    {
        // Arrange
        var identifyName = "test-subject";
        var member = Member.Create(identifyName, "John", null, "Doe", "Some description", "UTC", _clockMock);
        
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "strava-123", ["read"], _clockMock);
        
        _memberRepositoryMock.GetByIdentifyNameAsync(identifyName, Arg.Any<CancellationToken>())
            .Returns(member);

        var command = new DisconnectExternalProviderCommand(identifyName, ExternalProvider.Strava.Id, ExternalConnectionType.ActivitiesSync.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(member.HasActiveConnection(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync));
        
        await _memberRepositoryMock.Received(1).UpdateAsync(member, Arg.Any<CancellationToken>());
        await _memberRepositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var identifyName = "non-existent";
        _memberRepositoryMock.GetByIdentifyNameAsync(identifyName, Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        var command = new DisconnectExternalProviderCommand(identifyName, ExternalProvider.Strava.Id, ExternalConnectionType.ActivitiesSync.Id);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
