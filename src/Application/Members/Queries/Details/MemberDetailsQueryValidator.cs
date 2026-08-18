using FluentValidation;

namespace Application.Members.Queries.Details;

/// <summary>
/// Validates the initial payload required to create a member.
/// </summary>
public class MemberDetailsQueryValidator : AbstractValidator<MemberDetailsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberDetailsQueryValidator"/> class.
    /// </summary>
    public MemberDetailsQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithErrorCode("error_id_required");
    }
}
