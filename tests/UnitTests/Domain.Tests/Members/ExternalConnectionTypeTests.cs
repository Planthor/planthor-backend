using System;
using System.Reflection;
using Domain.Members;
using Xunit;

namespace Domain.Tests.Members;

public class ExternalConnectionTypeTests
{
    [Fact]
    public void FromId_ShouldReturnIdentity_WhenIdIsIDENTITY()
    {
        var result = ExternalConnectionType.FromId("IDENTITY");
        Assert.Equal(ExternalConnectionType.Identity, result);
    }

    [Fact]
    public void FromId_ShouldReturnActivitiesSync_WhenIdIsACTIVITIES_SYNC()
    {
        var result = ExternalConnectionType.FromId("ACTIVITIES_SYNC");
        Assert.Equal(ExternalConnectionType.ActivitiesSync, result);
    }

    [Fact]
    public void FromId_ShouldThrowArgumentException_WhenIdIsInvalid()
    {
        Assert.Throws<ArgumentException>(() => ExternalConnectionType.FromId("INVALID"));
    }

    [Fact]
    public void Constructor_EFCore_ShouldCreateEmpty()
    {
        var ctor = typeof(ExternalConnectionType).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        var instance = (ExternalConnectionType)ctor!.Invoke(null);
        Assert.Null(instance.Id);
        Assert.Null(instance.Name);
        Assert.Null(instance.I18NKey);
    }
}
