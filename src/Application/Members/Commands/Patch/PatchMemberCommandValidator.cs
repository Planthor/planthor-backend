using System;
using System.Linq;
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
        RuleFor(x => x.UpdateMask)
            .NotEmpty()
            .Must(mask => !mask.Any(field => string.Equals(field, "IdentifyName", StringComparison.OrdinalIgnoreCase)))
            .WithErrorCode("error_identify_name_read_only_should_be_updated_only_by_the_idp");
    }
}
