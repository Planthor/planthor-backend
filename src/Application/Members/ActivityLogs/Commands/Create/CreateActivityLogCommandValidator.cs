using FluentValidation;

namespace Application.Members.ActivityLogs.Commands.Create;

/// <summary>
/// Validator for the <see cref="CreateActivityLogCommand"/> to ensure required fields are provided.
/// </summary>
public sealed class CreateActivityLogCommandValidator : AbstractValidator<CreateActivityLogCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateActivityLogCommandValidator"/> class.
    /// Sets up the validation rules for the command.
    /// </summary>
    public CreateActivityLogCommandValidator()
    {
        RuleFor(v => v.PlanId).NotEmpty();
        RuleFor(v => v.ActivityLocalDate).NotEmpty();
    }
}
