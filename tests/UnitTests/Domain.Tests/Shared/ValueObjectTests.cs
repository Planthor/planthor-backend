using System.Collections.Generic;
using Domain.Shared;
using Xunit;

namespace Domain.Tests.Shared;

public class ValueObjectTests
{
    [Fact]
    public void WithSameValuesHaveSameHashCode()
    {
        // Arrange
        var valueObject1 = new TestValueObject
        {
            Value = 1
        };

        var valueObject2 = new TestValueObject
        {
            Value = 1
        };

        // Act & Assert
        Assert.Equal(valueObject1.GetHashCode(), valueObject2.GetHashCode());
    }

    [Fact]
    public void WithDifferentValuesHaveDifferentHashCode()
    {
        // Arrange
        var valueObject1 = new TestValueObject
        {
            Value = 1
        };

        var valueObject2 = new TestValueObject
        {
            Value = 2
        };

        // Act & Assert
        Assert.NotEqual(valueObject1.GetHashCode(), valueObject2.GetHashCode());
    }

    [Fact]
    public void WithSameValuesEqualsReturnTrue()
    {
        // Arrange
        var valueObject1 = new TestValueObject
        {
            Value = 1
        };

        var valueObject2 = new TestValueObject
        {
            Value = 1
        };

        // Act
        var result = valueObject1.Equals(valueObject2);

        // Act & Assert
        Assert.True(result);
    }

    [Fact]
    public void WithSameValuesEqualsReturnTrueShort()
    {
        // Arrange
        var valueObject1 = new TestValueObject
        {
            Value = 1
        };

        var valueObject2 = new TestValueObject
        {
            Value = 1
        };

        // Act
        var result = valueObject1 == valueObject2;

        // Act & Assert
        Assert.True(result);
    }

    [Fact]
    public void WithDifferentValuesEqualsReturnFalse()
    {
        // Arrange
        var valueObject1 = new TestValueObject
        {
            Value = 1
        };

        var valueObject2 = new TestValueObject
        {
            Value = 2
        };

        // Act
        var result = valueObject1.Equals(valueObject2);

        // Act & Assert
        Assert.False(result);
    }

    [Fact]
    public void WithDifferentTypesEqualsReturnFalse()
    {
        // Arrange
        var valueObject1 = new TestValueObject
        {
            Value = 1
        };

        int valueObject2 = 1;

        // Act
        var result = valueObject1.Equals(valueObject2);

        // Act & Assert
        Assert.False(result);
    }

    [Fact]
    public void WithNullObjectEqualsReturnFalse()
    {
        // Arrange
        TestValueObject valueObject1 = new()
        {
            Value = 1
        };

        TestValueObject valueObject2 = null!; // Cast null false positive.

        // Act
        var result = valueObject1.Equals(valueObject2);

        // Act & Assert
        Assert.False(result);
    }

    [Fact]
    public void InequalityOperator_DifferentValues_ReturnsTrue()
    {
        var a = new TestValueObject { Value = 1 };
        var b = new TestValueObject { Value = 2 };

        Assert.True(a != b);
    }

    [Fact]
    public void InequalityOperator_SameValues_ReturnsFalse()
    {
        var a = new TestValueObject { Value = 5 };
        var b = new TestValueObject { Value = 5 };

        Assert.False(a != b);
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
#pragma warning disable CS8604
        TestValueObject? left = null;
        TestValueObject? right = null;

        Assert.True(left == right);
#pragma warning restore CS8604
    }

    [Fact]
    public void EqualityOperator_LeftNull_ReturnsFalse()
    {
#pragma warning disable CS8604
        TestValueObject? left = null;
        var right = new TestValueObject { Value = 1 };

        Assert.False(left == right);
#pragma warning restore CS8604
    }

    private class TestValueObject : ValueObject
    {
        public int Value { get; init; }

        protected override IEnumerable<object> EqualityComponents => [Value];
    }
}
