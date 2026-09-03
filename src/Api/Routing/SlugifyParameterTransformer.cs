using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace Api.Routing;

/// <summary>
/// Transforms route parameter values to kebab-case (slug format).
/// This is typically used to ensure controller and action names in URLs
/// are formatted as kebab-case.
/// </summary>
public partial class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    private const string ReplacementPattern = "$1-$2";

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex KebabCaseRegex();

    /// <summary>
    /// Transforms the specified outbound route parameter value into a kebab-case string.
    /// </summary>
    /// <param name="value">The route parameter value to transform.</param>
    /// <returns>The transformed kebab-case string, or null if the value is null or empty.</returns>
    public string? TransformOutbound(object? value)
    {
        var stringValue = value?.ToString();
        if (string.IsNullOrEmpty(stringValue))
        {
            return null;
        }

        return KebabCaseRegex().Replace(stringValue, ReplacementPattern).ToUpperInvariant();
    }
}
