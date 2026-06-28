using System;
using Domain.Members;
using Xunit;

namespace Domain.Tests.Members;

public class ExternalProviderTests
{
    [Theory]
    [InlineData("STRAVA")]
    [InlineData("strava")]
    [InlineData("Strava")]
    public void FromId_Strava_ReturnsStrava(string id)
    {
        Assert.Equal(ExternalProvider.Strava, ExternalProvider.FromId(id));
    }

    [Theory]
    [InlineData("GITHUB")]
    [InlineData("github")]
    [InlineData("GitHub")]
    public void FromId_GitHub_ReturnsGitHub(string id)
    {
        Assert.Equal(ExternalProvider.GitHub, ExternalProvider.FromId(id));
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("GOOGLE")]
    public void FromId_InvalidId_ThrowsArgumentException(string id)
    {
        Assert.Throws<ArgumentException>(() => ExternalProvider.FromId(id));
    }

    [Fact]
    public void All_ContainsTwoProviders()
    {
        Assert.Equal(2, ExternalProvider.All.Count);
    }

    [Fact]
    public void All_ContainsStravaAndGitHub()
    {
        Assert.Contains(ExternalProvider.Strava, ExternalProvider.All);
        Assert.Contains(ExternalProvider.GitHub, ExternalProvider.All);
    }

    [Fact]
    public void Strava_HasCorrectProperties()
    {
        Assert.Equal("STRAVA", ExternalProvider.Strava.Id);
        Assert.Equal("Strava", ExternalProvider.Strava.Name);
        Assert.False(string.IsNullOrEmpty(ExternalProvider.Strava.I18NKey));
    }

    [Fact]
    public void GitHub_HasCorrectProperties()
    {
        Assert.Equal("GITHUB", ExternalProvider.GitHub.Id);
        Assert.Equal("GitHub", ExternalProvider.GitHub.Name);
        Assert.False(string.IsNullOrEmpty(ExternalProvider.GitHub.I18NKey));
    }
}
