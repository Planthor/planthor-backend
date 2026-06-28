using System;
using Domain.Shared;
using Xunit;

namespace Domain.Tests.Shared;

public class ValidationErrorTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var error = new ValidationError("firstName", "First name is required.", "REQUIRED");

        Assert.Equal("firstName", error.Field);
        Assert.Equal("First name is required.", error.Message);
        Assert.Equal("REQUIRED", error.Code);
    }

    [Theory]
    [InlineData("", "msg", "CODE")]
    [InlineData("  ", "msg", "CODE")]
    public void Constructor_BlankField_Throws(string field, string msg, string code)
    {
        Assert.Throws<ArgumentException>(() => new ValidationError(field, msg, code));
    }

    [Theory]
    [InlineData("field", "", "CODE")]
    [InlineData("field", "  ", "CODE")]
    public void Constructor_BlankMessage_Throws(string field, string msg, string code)
    {
        Assert.Throws<ArgumentException>(() => new ValidationError(field, msg, code));
    }

    [Theory]
    [InlineData("field", "msg", "")]
    [InlineData("field", "msg", "  ")]
    public void Constructor_BlankCode_Throws(string field, string msg, string code)
    {
        Assert.Throws<ArgumentException>(() => new ValidationError(field, msg, code));
    }

    [Fact]
    public void TwoErrorsWithSameValues_AreEqual()
    {
        var e1 = new ValidationError("f", "m", "C");
        var e2 = new ValidationError("f", "m", "C");

        Assert.Equal(e1, e2);
    }

    [Fact]
    public void TwoErrorsWithDifferentValues_AreNotEqual()
    {
        var e1 = new ValidationError("f1", "m", "C");
        var e2 = new ValidationError("f2", "m", "C");

        Assert.NotEqual(e1, e2);
    }

    [Fact]
    public void ToString_ContainsFieldMessageCode()
    {
        var error = new ValidationError("myField", "my message", "MY_CODE");
        var str = error.ToString();

        Assert.Contains("myField", str);
        Assert.Contains("my message", str);
        Assert.Contains("MY_CODE", str);
    }
}
