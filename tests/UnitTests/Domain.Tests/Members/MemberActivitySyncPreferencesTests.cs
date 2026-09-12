using System;
using Domain.Members;
using NodaTime;
using Xunit;

namespace Domain.Tests.Members;

public sealed class MemberActivitySyncPreferencesTests
{
    private sealed class TestClock(Instant instant) : IClock
    {
        public Instant GetCurrentInstant() => instant;
    }

    private static readonly IClock InitialClock = new TestClock(Instant.FromUtc(2026, 9, 1, 0, 0));
    private static Member CreateMember() => Member.Create("member", "Test", null, "Member", "", "UTC", false, InitialClock);

    [Fact]
    public void Create_WithoutPreference_DefaultsToFalse()
    {
        var member = CreateMember();

        Assert.False(member.AutoLinkUserAdapterToPlan);
    }

    [Fact]
    public void UpdateActivitySyncPreferences_ChangedPreference_UpdatesAuditButPreservesPlansAndConnections()
    {
        var member = CreateMember();
        member.SubscribeToPlan(Guid.NewGuid(), true, 1, false, InitialClock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "123", [], InitialClock);
        member.ClearDomainEvents();
        var later = new TestClock(Instant.FromUtc(2026, 9, 2, 0, 0));

        member.UpdateActivitySyncPreferences(true, later);

        Assert.True(member.AutoLinkUserAdapterToPlan);
        Assert.Equal(later.GetCurrentInstant(), member.LastUpdatedAt);
        Assert.Equal(member.Id, member.LastUpdatedBy);
        Assert.False(Assert.Single(member.PersonalPlans).LinkUserAdapter);
        Assert.Equal(ConnectionStatus.Active, Assert.Single(member.ExternalConnections).Status);
        Assert.Empty(member.DomainEvents);
    }

    [Fact]
    public void UpdateActivitySyncPreferences_RepeatedValue_PreservesAudit()
    {
        var member = CreateMember();
        member.UpdateActivitySyncPreferences(true, InitialClock);
        var later = new TestClock(Instant.FromUtc(2026, 9, 2, 0, 0));

        member.UpdateActivitySyncPreferences(true, later);

        Assert.Equal(InitialClock.GetCurrentInstant(), member.LastUpdatedAt);
    }

    [Fact]
    public void UpdateActivitySyncPreferences_DisableThenReconnect_PreservesMemberChoice()
    {
        var member = CreateMember();
        member.UpdateActivitySyncPreferences(true, InitialClock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "123", [], InitialClock);
        member.RevokeExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, InitialClock);
        Assert.True(member.AutoLinkUserAdapterToPlan);

        member.UpdateActivitySyncPreferences(false, InitialClock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "456", [], InitialClock);

        Assert.False(member.AutoLinkUserAdapterToPlan);
    }

    [Fact]
    public void UpdateActivitySyncPreferences_NullClock_RejectsBeforeMutation()
    {
        var member = CreateMember();

        Assert.Throws<ArgumentNullException>(() => member.UpdateActivitySyncPreferences(true, null!));

        Assert.False(member.AutoLinkUserAdapterToPlan);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ResolveLinkUserAdapterForNewPlan_InheritedChoice_ReturnsExpectedValue(
        bool memberDefault, bool expected)
    {
        var member = CreateMember();
        member.UpdateActivitySyncPreferences(memberDefault, InitialClock);

        var actual = member.ResolveLinkUserAdapterForNewPlan();

        Assert.Equal(expected, actual);
    }
}
