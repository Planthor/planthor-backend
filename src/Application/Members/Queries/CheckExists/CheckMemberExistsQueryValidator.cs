using FluentValidation;

namespace Application.Members.Queries.CheckExists;

/// <summary>
/// Validator for the <see cref="CheckMemberExistsQuery"/>.
/// </summary>
public class CheckMemberExistsQueryValidator : AbstractValidator<CheckMemberExistsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckMemberExistsQueryValidator"/> class.
    /// </summary>
    public CheckMemberExistsQueryValidator()
    {
        RuleFor(x => x.IdentifyName)
            .NotEmpty().WithErrorCode("error_identity_name_required");
    }
}
