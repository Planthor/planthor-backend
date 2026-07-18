using FluentValidation;

namespace Application.Members.Queries.ExternalConnections.List;

/// <summary>
/// Validator for <see cref="ListExternalConnectionsQuery"/>.
/// </summary>
public class ListExternalConnectionsQueryValidator : AbstractValidator<ListExternalConnectionsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListExternalConnectionsQueryValidator"/> class.
    /// </summary>
    public ListExternalConnectionsQueryValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
        RuleFor(x => x.CurrentIdentifyName).NotEmpty();
    }
}
