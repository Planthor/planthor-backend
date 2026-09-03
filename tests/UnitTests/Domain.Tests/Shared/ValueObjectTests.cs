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

    [Fact]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        // Arrange
        var valueObject = new TestValueObject { Value = 1 };

        // Act
        var result = valueObject.Equals(valueObject);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_WithDifferentValueObjectType_ReturnsFalse()
    {
        // Arrange
        ValueObject left = new TestValueObject { Value = 1 };
        ValueObject right = new OtherValueObject { Value = 1 };

        // Act
        var result = left.Equals(right);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_WithNullEqualityComponents_TreatsComponentsAsEmpty()
    {
        // Arrange
        var left = new NullComponentsValueObject();
        var right = new NullComponentsValueObject();

        // Act
        var result = left.Equals(right);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetHashCode_WithNullComponent_IncludesZeroComponentHash()
    {
        // Arrange
        var valueObject = new NullableComponentValueObject();

        // Act
        var result = valueObject.GetHashCode();

        // Assert
        Assert.Equal(17 * 31, result);
    }

    private class TestValueObject : ValueObject
    {
        public int Value { get; init; }

        protected override IEnumerable<object> EqualityComponents => [Value];
    }

    private sealed class OtherValueObject : ValueObject
    {
        public int Value { get; init; }

        protected override IEnumerable<object> EqualityComponents => [Value];
    }

    private sealed class NullComponentsValueObject : ValueObject
    {
        protected override IEnumerable<object> EqualityComponents => null!;
    }

    private sealed class NullableComponentValueObject : ValueObject
    {
        protected override IEnumerable<object> EqualityComponents => [null!];
    }
}
