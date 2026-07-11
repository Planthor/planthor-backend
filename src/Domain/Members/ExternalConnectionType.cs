using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Members;

/// <summary>
/// Represents the purpose or type of an external connection.
/// </summary>
/// <remarks>
/// Modelled as a smart-enum (class-based enumeration) to clearly distinguish
/// between different connection purposes (e.g., authentication vs. data synchronization)
/// for the same external provider.
/// </remarks>
public class ExternalConnectionType
{
    /// <summary>
    /// Connection used primarily for user authentication via Identity Brokering (e.g., Keycloak).
    /// </summary>
    public static readonly ExternalConnectionType Identity = new("IDENTITY", "Identity", "ConnectionType_Identity_Desc");

    /// <summary>
    /// Connection used for background synchronization of activities, logs, or plans (e.g., Strava).
    /// </summary>
    public static readonly ExternalConnectionType ActivitiesSync = new("ACTIVITIES_SYNC", "Activities Sync", "ConnectionType_ActivitiesSync_Desc");

    /// <summary>
    /// Gets the unique identifier for this connection type.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of this connection type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the localization key used by the i18n service to fetch the translated description.
    /// </summary>
    public string I18NKey { get; }

    // Required by EF Core
    private ExternalConnectionType() : this(default!, default!, default!) { }

    private ExternalConnectionType(string id, string name, string i18nKey)
    {
        Id = id;
        Name = name;
        I18NKey = i18nKey;
    }

    /// <summary>
    /// Retrieves an <see cref="ExternalConnectionType"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The identifier (e.g., "IDENTITY").</param>
    /// <returns>The matching <see cref="ExternalConnectionType"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid type.</exception>
    public static ExternalConnectionType FromId(string id)
    {
        return All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid ExternalConnectionType identifier.");
    }

    /// <summary>
    /// Returns a collection of all available <see cref="ExternalConnectionType"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<ExternalConnectionType> All => [Identity, ActivitiesSync];
}
