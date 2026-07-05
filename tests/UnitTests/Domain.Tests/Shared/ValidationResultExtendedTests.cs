using System.Collections.Generic;
using Domain.Shared;
using Xunit;

namespace Domain.Tests.Shared;

public class ValidationResultExtendedTests
{
    [Fact]
    public void IsValid_WithNoErrors_ReturnsTrue()
    {
        var result = new ValidationResult(new List<ValidationError>().AsReadOnly());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void IsValid_WithErrors_ReturnsFalse()
    {
        var errors = new List<ValidationError>
        {
            new("field", "message", "CODE")
        }.AsReadOnly();
        var result = new ValidationResult(errors);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ToString_WhenValid_ReturnsValidString()
    {
        var result = new ValidationResult(new List<ValidationError>().AsReadOnly());

        Assert.Contains("true", result.ToString());
    }

    [Fact]
    public void ToString_WhenInvalid_ContainsErrorInfo()
    {
        var errors = new List<ValidationError>
        {
            new("firstName", "Required", "REQUIRED_FIRST_NAME")
        }.AsReadOnly();
        var result = new ValidationResult(errors);

        var str = result.ToString();
        Assert.Contains("false", str);
        Assert.Contains("firstName", str);
    }

    [Fact]
    public void ToString_WithMultipleErrors_ContainsAllErrors()
    {
        var errors = new List<ValidationError>
        {
            new("field1", "msg1", "CODE1"),
            new("field2", "msg2", "CODE2")
        }.AsReadOnly();
        var result = new ValidationResult(errors);

        var str = result.ToString();
        Assert.Contains("field1", str);
        Assert.Contains("field2", str);
    }
}
