using Domain.Members;
using Xunit;

namespace Domain.Tests.Members;

public class ExternalActivitySourceTests
{
    [Fact]
    public void SamePoviderAndId_AreEqual()
    {
        var source1 = new ExternalActivitySource(ExternalProvider.Strava, "12345");
        var source2 = new ExternalActivitySource(ExternalProvider.Strava, "12345");

        Assert.Equal(source1, source2);
    }

    [Fact]
    public void DifferentId_AreNotEqual()
    {
        var source1 = new ExternalActivitySource(ExternalProvider.Strava, "12345");
        var source2 = new ExternalActivitySource(ExternalProvider.Strava, "99999");

        Assert.NotEqual(source1, source2);
    }

    [Fact]
    public void DifferentProvider_AreNotEqual()
    {
        var source1 = new ExternalActivitySource(ExternalProvider.Strava, "12345");
        var source2 = new ExternalActivitySource(ExternalProvider.GitHub, "12345");

        Assert.NotEqual(source1, source2);
    }

    [Fact]
    public void EqualityOperator_SameValues_ReturnsTrue()
    {
        var source1 = new ExternalActivitySource(ExternalProvider.Strava, "abc");
        var source2 = new ExternalActivitySource(ExternalProvider.Strava, "abc");

        Assert.True(source1 == source2);
    }

    [Fact]
    public void InequalityOperator_DifferentValues_ReturnsTrue()
    {
        var source1 = new ExternalActivitySource(ExternalProvider.Strava, "abc");
        var source2 = new ExternalActivitySource(ExternalProvider.GitHub, "abc");

        Assert.True(source1 != source2);
    }

    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var source = new ExternalActivitySource(ExternalProvider.GitHub, "commit-sha");

        Assert.Equal(ExternalProvider.GitHub, source.Provider);
        Assert.Equal("commit-sha", source.ExternalActivityId);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var source1 = new ExternalActivitySource(ExternalProvider.Strava, "xyz");
        var source2 = new ExternalActivitySource(ExternalProvider.Strava, "xyz");

        Assert.Equal(source1.GetHashCode(), source2.GetHashCode());
    }
}
