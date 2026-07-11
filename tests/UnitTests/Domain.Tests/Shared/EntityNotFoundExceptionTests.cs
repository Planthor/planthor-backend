using System;
using Domain.Members;
using Domain.Shared.Exceptions;
using Xunit;

namespace Domain.Tests.Shared;

public class EntityNotFoundExceptionTests
{
    [Fact]
    public void DefaultConstructor_MessageContainsEntityTypeName()
    {
        var ex = new EntityNotFoundException<Member, Guid>();

        Assert.Contains("Member", ex.Message);
        Assert.Equal(Guid.Empty, ex.EntityId);
        Assert.Equal("Member", ex.EntityType);
    }

    [Fact]
    public void IdConstructor_SetsEntityIdAndMessage()
    {
        var id = Guid.NewGuid();

        var ex = new EntityNotFoundException<Member, Guid>(id);

        Assert.Equal(id, ex.EntityId);
        Assert.Contains(id.ToString(), ex.Message);
        Assert.Contains("Member", ex.Message);
    }

    [Fact]
    public void CustomMessageConstructor_UsesCustomMessage()
    {
        var id = Guid.NewGuid();

        var ex = new EntityNotFoundException<Member, Guid>("Custom message", id);

        Assert.Equal("Custom message", ex.Message);
        Assert.Equal(id, ex.EntityId);
    }

    [Fact]
    public void InnerExceptionConstructor_WrapsInnerException()
    {
        var id = Guid.NewGuid();
        var inner = new InvalidOperationException("inner");

        var ex = new EntityNotFoundException<Member, Guid>(id, inner);

        Assert.Equal(inner, ex.InnerException);
        Assert.Equal(id, ex.EntityId);
    }

    [Fact]
    public void IsException_CanBeCaughtAsException()
    {
        var ex = new EntityNotFoundException<Member, Guid>(Guid.NewGuid());

        Assert.IsType<Exception>(ex, exactMatch: false);
    }
}
