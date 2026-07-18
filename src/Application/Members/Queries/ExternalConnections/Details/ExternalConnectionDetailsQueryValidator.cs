using FluentValidation;

namespace Application.Members.Queries.ExternalConnections.Details;

/// <summary>
/// Validator for <see cref="ExternalConnectionDetailsQuery"/>.
/// </summary>
public class ExternalConnectionDetailsQueryValidator : AbstractValidator<ExternalConnectionDetailsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalConnectionDetailsQueryValidator"/> class.
    /// </summary>
    public ExternalConnectionDetailsQueryValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
        RuleFor(x => x.CurrentIdentifyName).NotEmpty();
        RuleFor(x => x.ConnectionId).NotEmpty();
    }
}
