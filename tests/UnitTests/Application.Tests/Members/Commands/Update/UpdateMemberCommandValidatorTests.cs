using System;
using Application.Members.Commands.Update;

namespace Application.Tests.Members.Commands.Update;

public class UpdateMemberCommandValidatorTests
{
    private readonly UpdateMemberCommandValidator _validator = new();

    private static UpdateMemberCommand Valid() =>
        new(Guid.NewGuid(), "John", null, "Doe", null, null, "UTC");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyFirstName_Fails(string value)
    {
        var result = _validator.Validate(Valid() with { FirstName = value });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMemberCommand.FirstName));
    }

    [Fact]
    public void Validate_FirstNameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { FirstName = new string('A', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMemberCommand.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyLastName_Fails(string value)
    {
        var result = _validator.Validate(Valid() with { LastName = value });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMemberCommand.LastName));
    }

    [Fact]
    public void Validate_LastNameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { LastName = new string('Z', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMemberCommand.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Not/A/Timezone")]
    public void Validate_InvalidTimezone_Fails(string tz)
    {
        var result = _validator.Validate(Valid() with { PreferredTimezone = tz });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMemberCommand.PreferredTimezone));
    }

    [Fact]
    public void Validate_ValidIanaTimezone_Passes()
    {
        var result = _validator.Validate(Valid() with { PreferredTimezone = "Asia/Ho_Chi_Minh" });

        Assert.True(result.IsValid);
    }
}
