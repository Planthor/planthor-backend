using Application.Members.Commands.Create;

namespace Application.Tests.Members.Commands.Create;

public class CreateMemberCommandValidatorTests
{
    private readonly CreateMemberCommandValidator _validator = new();

    private static CreateMemberCommand Valid() =>
        new("user1", "John", null, "Doe", null, "UTC");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyIdentifyName_Fails(string value)
    {
        var result = _validator.Validate(Valid() with { IdentifyName = value });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMemberCommand.IdentifyName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyFirstName_Fails(string value)
    {
        var result = _validator.Validate(Valid() with { FirstName = value });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMemberCommand.FirstName));
    }

    [Fact]
    public void Validate_FirstNameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { FirstName = new string('A', 101) });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMemberCommand.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyLastName_Fails(string value)
    {
        var result = _validator.Validate(Valid() with { LastName = value });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMemberCommand.LastName));
    }

    [Fact]
    public void Validate_LastNameTooLong_Fails()
    {
        var result = _validator.Validate(Valid() with { LastName = new string('Z', 101) });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMemberCommand.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Not/A/Timezone")]
    public void Validate_InvalidTimezone_Fails(string tz)
    {
        var result = _validator.Validate(Valid() with { PreferredTimezone = tz });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMemberCommand.PreferredTimezone));
    }

    [Fact]
    public void Validate_ValidIanaTimezone_Passes()
    {
        var result = _validator.Validate(Valid() with { PreferredTimezone = "Asia/Ho_Chi_Minh" });
        Assert.True(result.IsValid);
    }
}
