using System;
using Domain.Members;
using Xunit;

namespace Domain.Tests.Members;

public class ConnectionStatusTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("a")]
    public void FromId_Active_ReturnsActive(string id)
    {
        Assert.Equal(ConnectionStatus.Active, ConnectionStatus.FromId(id));
    }

    [Theory]
    [InlineData("R")]
    [InlineData("r")]
    public void FromId_Revoked_ReturnsRevoked(string id)
    {
        Assert.Equal(ConnectionStatus.Revoked, ConnectionStatus.FromId(id));
    }

    [Theory]
    [InlineData("E")]
    [InlineData("e")]
    public void FromId_Expired_ReturnsExpired(string id)
    {
        Assert.Equal(ConnectionStatus.Expired, ConnectionStatus.FromId(id));
    }

    [Theory]
    [InlineData("X")]
    [InlineData("")]
    [InlineData("INVALID")]
    public void FromId_InvalidId_ThrowsArgumentException(string id)
    {
        Assert.Throws<ArgumentException>(() => ConnectionStatus.FromId(id));
    }

    [Fact]
    public void All_ContainsThreeStatuses()
    {
        Assert.Equal(3, ConnectionStatus.All.Count);
    }

    [Fact]
    public void All_ContainsAllExpectedStatuses()
    {
        Assert.Contains(ConnectionStatus.Active, ConnectionStatus.All);
        Assert.Contains(ConnectionStatus.Revoked, ConnectionStatus.All);
        Assert.Contains(ConnectionStatus.Expired, ConnectionStatus.All);
    }

    [Fact]
    public void Active_HasCorrectProperties()
    {
        Assert.Equal("A", ConnectionStatus.Active.Id);
        Assert.Equal("ACTIVE", ConnectionStatus.Active.Name);
        Assert.False(string.IsNullOrEmpty(ConnectionStatus.Active.I18NKey));
    }

    [Fact]
    public void Revoked_HasCorrectProperties()
    {
        Assert.Equal("R", ConnectionStatus.Revoked.Id);
    }

    [Fact]
    public void Expired_HasCorrectProperties()
    {
        Assert.Equal("E", ConnectionStatus.Expired.Id);
    }
}
