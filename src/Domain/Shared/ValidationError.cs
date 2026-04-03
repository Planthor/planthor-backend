using System;
using System.Collections.Generic;

namespace Domain.Shared;

/// <summary>
/// Represents a single validation error produced during aggregate validation.
/// </summary>
/// <remarks>
/// Implemented as a <see cref="ValueObject"/> since a validation error has no
/// identity — two errors with the same <see cref="Field"/>, <see cref="Message"/>,
/// and <see cref="Code"/> are considered identical by definition.
/// <para>
/// Structural equality, <c>GetHashCode</c>, and <c>==</c> / <c>!=</c> operators
/// are inherited from <see cref="ValueObject"/> — no manual implementation needed.
/// </para>
/// </remarks>
public sealed class ValidationError : ValueObject
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationError"/>.
    /// </summary>
    /// <param name="field">
    /// The name of the field that failed validation.
    /// Must not be null or whitespace.
    /// </param>
    /// <param name="message">
    /// A human-readable description of the validation failure.
    /// Must not be null or whitespace.
    /// </param>
    /// <param name="code">
    /// A machine-readable error code for programmatic client handling.
    /// Must not be null or whitespace.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when any parameter is null, empty, or whitespace.
    /// </exception>
    public ValidationError(string field, string message, string code)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("Field must not be null or whitespace.", nameof(field));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message must not be null or whitespace.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code must not be null or whitespace.", nameof(code));
        }

        Field = field;
        Message = message;
        Code = code;
    }

    /// <summary>
    /// Gets the name of the field or property that failed validation.
    /// Should match the API contract field name for client-side mapping.
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// Gets the human-readable message describing why validation failed.
    /// Intended for display to the end user.
    /// </summary>
    /// <example>
    /// <c>"Must be greater than zero."</c>
    /// </example>
    public string Message { get; }

    /// <summary>
    /// Gets the machine-readable error code for programmatic handling.
    /// Allows the client to map errors to localized strings
    /// without depending on the message text.
    /// </summary>
    /// <example>
    /// <c>"INVALID_TARGET"</c>, <c>"MISSING_TIMEZONE"</c>, <c>"INVALID_DATE_RANGE"</c>
    /// </example>
    public string Code { get; }

    /// <inheritdoc/>
    protected override IEnumerable<object> EqualityComponents
    {
        get
        {
            yield return Field;
            yield return Message;
            yield return Code;
        }
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"ValidationError {{ Field = {Field}, Message = {Message}, Code = {Code} }}";
}
