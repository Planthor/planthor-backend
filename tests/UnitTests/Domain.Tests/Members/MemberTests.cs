using System;
using System.Collections.Generic;
using Domain.Members;
using Domain.Shared;
using NodaTime;
using Xunit;

namespace Domain.Tests.Members;

public class MemberTests
{
    private static readonly IClock Clock = new TestClock();

    private static Member CreateMember(string identifyName = "user1") =>
        Member.Create(identifyName, "John", "", "Doe", "", "UTC", Clock);

    private class TestClock : IClock
    {
        public Instant GetCurrentInstant() => Instant.FromUtc(2024, 1, 1, 0, 0);
    }

    // --- Create ---

    [Fact]
    public void Create_WithValidParams_ReturnsMemberWithCorrectProperties()
    {
        var member = Member.Create("user1", "John", "M", "Doe", "Bio", "UTC", Clock);

        Assert.NotEqual(Guid.Empty, member.Id);
        Assert.Equal("user1", member.IdentifyName);
        Assert.Equal("John", member.FirstName);
        Assert.Equal("M", member.MiddleName);
        Assert.Equal("Doe", member.LastName);
        Assert.Equal("Bio", member.Description);
        Assert.Equal("UTC", member.PreferredTimezone);
        Assert.Null(member.PathAvatar);
    }

    [Fact]
    public void Create_RaisesMemberRegisteredEvent()
    {
        var member = Member.Create("user1", "John", "", "Doe", "", "UTC", Clock);

        Assert.Single(member.DomainEvents);
    }

    [Fact]
    public void Create_TwoMembers_HaveDifferentIds()
    {
        var m1 = CreateMember("user1");
        var m2 = CreateMember("user2");

        Assert.NotEqual(m1.Id, m2.Id);
    }

    // --- Validate ---

    [Fact]
    public void Validate_WithValidMember_ReturnsValid()
    {
        var member = CreateMember();

        var result = member.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyFirstName_ReturnsError()
    {
        var member = Member.Create("user1", "", "", "Doe", "", "UTC", Clock);

        var result = member.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "REQUIRED_FIRST_NAME");
    }

    [Fact]
    public void Validate_EmptyLastName_ReturnsError()
    {
        var member = Member.Create("user1", "John", "", "", "", "UTC", Clock);

        var result = member.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "REQUIRED_LAST_NAME");
    }

    [Fact]
    public void Validate_EmptyTimezone_ReturnsError()
    {
        var member = Member.Create("user1", "John", "", "Doe", "", "", Clock);

        var result = member.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "REQUIRED_TIMEZONE");
    }

    [Fact]
    public void Validate_InvalidTimezone_ReturnsError()
    {
        var member = Member.Create("user1", "John", "", "Doe", "", "Not/Valid", Clock);

        var result = member.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_TIMEZONE");
    }

    [Fact]
    public void Validate_MultipleInvalid_ReturnsMultipleErrors()
    {
        var member = Member.Create("user1", "", "", "", "", "", Clock);

        var result = member.Validate();

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }

    // --- UpdateAvatar ---

    [Fact]
    public void UpdateAvatar_SetsPathAvatar()
    {
        var member = CreateMember();
        member.UpdateAvatar("https://storage.example.com/avatar.jpg", Clock);

        Assert.Equal("https://storage.example.com/avatar.jpg", member.PathAvatar);
    }

    [Fact]
    public void UpdateAvatar_RaisesMemberAvatarUpdatedEvent()
    {
        var member = CreateMember();
        member.ClearDomainEvents();

        member.UpdateAvatar("https://storage.example.com/avatar.jpg", Clock);

        Assert.Single(member.DomainEvents);
    }

    [Fact]
    public void UpdateAvatar_WithExistingAvatar_RaisesEventWithOldUri()
    {
        var member = CreateMember();
        member.UpdateAvatar("https://storage.example.com/old.jpg", Clock);
        member.ClearDomainEvents();

        member.UpdateAvatar("https://storage.example.com/new.jpg", Clock);

        Assert.Single(member.DomainEvents);
        Assert.Equal("https://storage.example.com/new.jpg", member.PathAvatar);
    }

    // --- ConnectExternalProvider ---

    [Fact]
    public void ConnectExternalProvider_NewProvider_AddsConnection()
    {
        var member = CreateMember();

        member.ConnectExternalProvider(ExternalProvider.Strava, "strava123", ["read"], Clock);

        Assert.Single(member.ExternalConnections);
        Assert.Equal(ConnectionStatus.Active, member.ExternalConnections[0].Status);
    }

    [Fact]
    public void ConnectExternalProvider_ActiveAlreadyExists_Throws()
    {
        var member = CreateMember();
        member.ConnectExternalProvider(ExternalProvider.Strava, "strava123", ["read"], Clock);

        Assert.Throws<InvalidOperationException>(() =>
            member.ConnectExternalProvider(ExternalProvider.Strava, "strava999", ["read"], Clock));
    }

    [Fact]
    public void ConnectExternalProvider_RevokedExists_Reactivates()
    {
        var member = CreateMember();
        member.ConnectExternalProvider(ExternalProvider.Strava, "strava123", ["read"], Clock);
        member.RevokeExternalProvider(ExternalProvider.Strava, Clock);

        member.ConnectExternalProvider(ExternalProvider.Strava, "strava456", ["read", "write"], Clock);

        Assert.Single(member.ExternalConnections);
        Assert.Equal(ConnectionStatus.Active, member.ExternalConnections[0].Status);
    }

    [Fact]
    public void ConnectExternalProvider_RaisesEvent()
    {
        var member = CreateMember();
        member.ClearDomainEvents();

        member.ConnectExternalProvider(ExternalProvider.GitHub, "gh123", [], Clock);

        Assert.Single(member.DomainEvents);
    }

    // --- RevokeExternalProvider ---

    [Fact]
    public void RevokeExternalProvider_ActiveConnection_RevokesIt()
    {
        var member = CreateMember();
        member.ConnectExternalProvider(ExternalProvider.Strava, "strava123", ["read"], Clock);

        member.RevokeExternalProvider(ExternalProvider.Strava, Clock);

        Assert.Equal(ConnectionStatus.Revoked, member.ExternalConnections[0].Status);
    }

    [Fact]
    public void RevokeExternalProvider_NoActiveConnection_Throws()
    {
        var member = CreateMember();

        Assert.Throws<InvalidOperationException>(() =>
            member.RevokeExternalProvider(ExternalProvider.Strava, Clock));
    }

    [Fact]
    public void RevokeExternalProvider_RaisesEvent()
    {
        var member = CreateMember();
        member.ConnectExternalProvider(ExternalProvider.Strava, "strava123", ["read"], Clock);
        member.ClearDomainEvents();

        member.RevokeExternalProvider(ExternalProvider.Strava, Clock);

        Assert.Single(member.DomainEvents);
    }

    // --- HasActiveConnection ---

    [Fact]
    public void HasActiveConnection_WhenActive_ReturnsTrue()
    {
        var member = CreateMember();
        member.ConnectExternalProvider(ExternalProvider.Strava, "strava123", [], Clock);

        Assert.True(member.HasActiveConnection(ExternalProvider.Strava));
    }

    [Fact]
    public void HasActiveConnection_WhenNone_ReturnsFalse()
    {
        var member = CreateMember();

        Assert.False(member.HasActiveConnection(ExternalProvider.Strava));
    }

    [Fact]
    public void HasActiveConnection_WhenRevoked_ReturnsFalse()
    {
        var member = CreateMember();
        member.ConnectExternalProvider(ExternalProvider.Strava, "strava123", [], Clock);
        member.RevokeExternalProvider(ExternalProvider.Strava, Clock);

        Assert.False(member.HasActiveConnection(ExternalProvider.Strava));
    }

    // --- SubscribeToPlan ---

    [Fact]
    public void SubscribeToPlan_NewPlan_AddsPersonalPlan()
    {
        var member = CreateMember();
        var planId = Guid.NewGuid();

        member.SubscribeToPlan(planId, true, 1, false, Clock);

        Assert.Single(member.PersonalPlans);
        Assert.Equal(planId, member.PersonalPlans[0].PlanId);
    }

    [Fact]
    public void SubscribeToPlan_DuplicatePlan_Throws()
    {
        var member = CreateMember();
        var planId = Guid.NewGuid();
        member.SubscribeToPlan(planId, true, 1, false, Clock);

        Assert.Throws<InvalidOperationException>(() =>
            member.SubscribeToPlan(planId, false, 2, true, Clock));
    }

    [Fact]
    public void SubscribeToPlan_RaisesEvent()
    {
        var member = CreateMember();
        member.ClearDomainEvents();

        member.SubscribeToPlan(Guid.NewGuid(), true, 0, false, Clock);

        Assert.Single(member.DomainEvents);
    }

    // --- UnsubscribeFromPlan ---

    [Fact]
    public void UnsubscribeFromPlan_ExistingPlan_RemovesPlan()
    {
        var member = CreateMember();
        var planId = Guid.NewGuid();
        member.SubscribeToPlan(planId, true, 1, false, Clock);

        member.UnsubscribeFromPlan(planId, Clock);

        Assert.Empty(member.PersonalPlans);
    }

    [Fact]
    public void UnsubscribeFromPlan_NotSubscribed_Throws()
    {
        var member = CreateMember();

        Assert.Throws<InvalidOperationException>(() =>
            member.UnsubscribeFromPlan(Guid.NewGuid(), Clock));
    }
}
