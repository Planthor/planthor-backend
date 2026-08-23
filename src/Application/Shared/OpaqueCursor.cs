using System;
using System.Text;

namespace Application.Shared;

/// <summary>
/// Provides utility methods to encode and decode pagination cursors into opaque strings (Base64).
/// This prevents clients from predicting or manually generating cursor values.
/// </summary>
public static class OpaqueCursor
{
    /// <summary>
    /// Encodes a plain-text cursor into an opaque Base64 string.
    /// </summary>
    /// <param name="plainCursor">The plain text cursor to encode.</param>
    /// <returns>The Base64 encoded string.</returns>
    public static string Encode(string plainCursor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainCursor);
        var plainBytes = Encoding.UTF8.GetBytes(plainCursor);
        return Convert.ToBase64String(plainBytes);
    }

    /// <summary>
    /// Decodes an opaque Base64 string back into a plain-text cursor.
    /// Returns null if the string is not valid Base64.
    /// </summary>
    /// <param name="encodedCursor">The Base64 encoded cursor.</param>
    /// <returns>The decoded plain text cursor, or null if invalid.</returns>
    public static string? Decode(string? encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return null;
        }

        try
        {
            var base64Bytes = Convert.FromBase64String(encodedCursor);
            return Encoding.UTF8.GetString(base64Bytes);
        }
        catch (FormatException)
        {
            return null; // Ignore invalid format and fallback to no cursor
        }
    }
}
