using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanthorWebApi.Domain.ExternalConnections;

/// <summary>
/// Represents an external service provider that can be linked to a member's account.
/// </summary>
/// <remarks>
/// Modelled as a smart-enum (class-based enumeration) to support i18n through
/// localization keys, while keeping the set of valid providers closed and type-safe.
/// Follows the same pattern as <see cref="Plans.PlanStatus"/>.
/// </remarks>
public class ExternalProvider
{
    /// <summary>
    /// Strava fitness tracking platform.
    /// </summary>
    public static readonly ExternalProvider Strava = new("STRAVA", "Strava", "ExternalProvider_Strava_Desc");

    /// <summary>
    /// GitHub source code platform.
    /// </summary>
    public static readonly ExternalProvider GitHub = new("GITHUB", "GitHub", "ExternalProvider_GitHub_Desc");

    /// <summary>
    /// Gets the unique identifier for this external provider.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of this external provider.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the localization key used by the i18n service to fetch the translated description.
    /// </summary>
    public string I18NKey { get; }

    private ExternalProvider(string id, string name, string descriptionI18nKey)
    {
        Id = id;
        Name = name;
        I18NKey = descriptionI18nKey;
    }

    /// <summary>
    /// Retrieves an <see cref="ExternalProvider"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The identifier (e.g., "STRAVA").</param>
    /// <returns>The matching <see cref="ExternalProvider"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid provider.</exception>
    public static ExternalProvider FromId(string id)
    {
        return All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid ExternalProvider identifier.");
    }

    /// <summary>
    /// Returns a collection of all available <see cref="ExternalProvider"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<ExternalProvider> All => [Strava, GitHub];
}
