using System;
using System.Collections.Generic;
using Domain.Shared;

namespace Domain.Shared.Exceptions;

/// <summary>
/// Exception thrown when an aggregate fails its invariant validation.
/// </summary>
public sealed class DomainValidationException : Exception
{
    /// <summary>
    /// Gets the structured list of validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DomainValidationException"/>
    /// from a <see cref="ValidationResult"/>.
    /// </summary>
    /// <param name="result">
    /// The failed validation result containing one or more errors.
    /// </param>
    public DomainValidationException(ValidationResult result)
        : base("One or more domain validation errors occurred.")
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        Errors = result.Errors;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DomainValidationException"/>
    /// from an explicit list of errors.
    /// </summary>
    /// <param name="errors">The validation errors that caused this exception.</param>
    public DomainValidationException(IReadOnlyList<ValidationError> errors)
        : base("One or more domain validation errors occurred.")
    {
        Errors = errors;
    }
}
