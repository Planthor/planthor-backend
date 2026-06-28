using System;
using System.Collections.Generic;
using Domain.Shared;
using Domain.Shared.Exceptions;
using Xunit;

namespace Domain.Tests.Shared;

public class DomainValidationExceptionTests
{
    [Fact]
    public void Constructor_FromValidationResult_SetsErrors()
    {
        var errors = new List<ValidationError>
        {
            new("field", "message", "CODE")
        }.AsReadOnly();
        var result = new ValidationResult(errors);

        var ex = new DomainValidationException(result);

        Assert.Single(ex.Errors);
        Assert.Equal("field", ex.Errors[0].Field);
    }

    [Fact]
    public void Constructor_FromValidationResult_HasExpectedMessage()
    {
        var result = new ValidationResult(new List<ValidationError>().AsReadOnly());

        var ex = new DomainValidationException(result);

        Assert.Contains("domain validation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_FromValidationResult_NullThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DomainValidationException((ValidationResult)null!));
    }

    [Fact]
    public void Constructor_FromErrorList_SetsErrors()
    {
        var errors = new List<ValidationError>
        {
            new("f1", "m1", "C1"),
            new("f2", "m2", "C2")
        }.AsReadOnly();

        var ex = new DomainValidationException(errors);

        Assert.Equal(2, ex.Errors.Count);
    }

    [Fact]
    public void IsException_CanBeCaughtAsException()
    {
        var errors = new List<ValidationError> { new("f", "m", "C") }.AsReadOnly();
        var ex = new DomainValidationException(errors);

        Assert.IsAssignableFrom<Exception>(ex);
    }
}
