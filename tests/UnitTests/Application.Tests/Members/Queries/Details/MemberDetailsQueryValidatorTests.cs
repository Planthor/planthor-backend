using System;
using Application.Members.Queries.Details;

namespace Application.Tests.Members.Queries.Details;

public class MemberDetailsQueryValidatorTests
{
    private readonly MemberDetailsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidId_Passes()
    {
        var result = _validator.Validate(new MemberDetailsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new MemberDetailsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(MemberDetailsQuery.Id));
    }
}
