using FluentValidation;

namespace Application.Members.Commands.Patch;

/// <summary>
/// Validates the payload required to patch a member.
/// </summary>
public sealed class PatchMemberCommandValidator : AbstractValidator<PatchMemberCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PatchMemberCommandValidator"/> class.
    /// </summary>
    public PatchMemberCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UpdateMask).NotEmpty();
    }
}
