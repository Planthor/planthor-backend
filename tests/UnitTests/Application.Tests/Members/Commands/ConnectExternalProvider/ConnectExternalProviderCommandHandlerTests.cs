using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Commands.ConnectExternalProvider;
using Domain.Members;
using NSubstitute;
using NodaTime;

namespace Application.Tests.Members.Commands.ConnectExternalProvider;

public class ConnectExternalProviderCommandHandlerTests
{
    private readonly IMemberRepository _memberRepositoryMock;
    private readonly IClock _clockMock;
    private readonly ConnectExternalProviderCommandHandler _handler;

    public ConnectExternalProviderCommandHandlerTests()
    {
        _memberRepositoryMock = Substitute.For<IMemberRepository>();
        _clockMock = Substitute.For<IClock>();
        _handler = new ConnectExternalProviderCommandHandler(_memberRepositoryMock, _clockMock);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMemberRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ConnectExternalProviderCommandHandler(null!, _clockMock));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenClockIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ConnectExternalProviderCommandHandler(_memberRepositoryMock, null!));
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenMemberNotFound()
    {
        // Arrange
        var command = new ConnectExternalProviderCommand("user1", "strava", "sync", "ext_123", ["read_all"]);
        _memberRepositoryMock.GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains(command.IdentifyName, ex.Message);
    }

    [Fact]
    public async Task Handle_ShouldConnectExternalProvider_WhenMemberExists()
    {
        // Arrange
        var command = new ConnectExternalProviderCommand("user1", ExternalProvider.Strava.Id, ExternalConnectionType.ActivitiesSync.Id, "ext_123", ["read_all"]);
        var clock = SystemClock.Instance;
        var member = Member.Create("user1", "John", "Doe", "Smith", "desc", "UTC", clock);

        _memberRepositoryMock.GetByIdentifyNameAsync(command.IdentifyName, Arg.Any<CancellationToken>())
            .Returns(member);
        
        _clockMock.GetCurrentInstant().Returns(clock.GetCurrentInstant());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _memberRepositoryMock.Received(1).UpdateAsync(member, Arg.Any<CancellationToken>());
        await _memberRepositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
