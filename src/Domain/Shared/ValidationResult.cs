
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Shared;

/// <summary>
/// Represents the outcome of a domain validation operation.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ValidationResult"/>.
/// </remarks>
/// <param name="errors">
/// The list of validation errors. Pass an empty list for a successful result.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="errors"/> is null.
/// </exception>
public sealed class ValidationResult(IReadOnlyList<ValidationError> errors)
{
    /// <summary>
    /// Gets the list of validation errors.
    /// Empty when <see cref="IsValid"/> is <c>true</c>.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; } = errors ?? throw new ArgumentNullException(nameof(errors));

    /// <summary>
    /// Gets whether the validation passed with no errors.
    /// </summary>
    public bool IsValid
    {
        get
        {
            return this.Errors.Count == 0;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsValid)
        {
            return "ValidationResult { IsValid = true }";
        }

        var sb = new StringBuilder();
        sb.Append("ValidationResult { IsValid = false, Errors = [");

        for (int i = 0; i < Errors.Count; i++)
        {
            sb.Append(Errors[i]);
            if (i < Errors.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append("] }");
        return sb.ToString();
    }
}
