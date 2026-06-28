using Application.Members.Queries.CheckExists;

namespace Application.Tests.Members.Queries.CheckExists;

public class CheckMemberExistsQueryValidatorTests
{
    private readonly CheckMemberExistsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidIdentifyName_Passes()
    {
        var result = _validator.Validate(new CheckMemberExistsQuery("user1"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyIdentifyName_Fails(string value)
    {
        var result = _validator.Validate(new CheckMemberExistsQuery(value));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CheckMemberExistsQuery.IdentifyName));
    }
}
