using FluentValidation;

namespace Application.ExternalSync.Queries.GetExternalActivitySyncStatus;

/// <summary>Validates external activity sync status queries.</summary>
public sealed class GetExternalActivitySyncStatusQueryValidator
    : AbstractValidator<GetExternalActivitySyncStatusQuery>
{
    /// <summary>Defines required owner and provider fields.</summary>
    public GetExternalActivitySyncStatusQueryValidator()
    {
        RuleFor(query => query.Identifier).NotEmpty();
        RuleFor(query => query.CurrentIdentifyName).NotEmpty();
        RuleFor(query => query.ProviderId).NotEmpty();
    }
}
