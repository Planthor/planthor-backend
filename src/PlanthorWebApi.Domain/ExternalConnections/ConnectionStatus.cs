using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanthorWebApi.Domain.ExternalConnections;

/// <summary>
/// Represents the lifecycle status of an external service connection.
/// </summary>
/// <remarks>
/// Modelled as a smart-enum (class-based enumeration) to support i18n through
/// localization keys, while keeping the set of valid statuses closed and type-safe.
/// Follows the same pattern as <see cref="Plans.PlanStatus"/>.
/// </remarks>
public class ConnectionStatus
{
    /// <summary>
    /// The connection is active and tokens are valid.
    /// </summary>
    public static readonly ConnectionStatus Active = new("A", "ACTIVE", "ConnectionStatus_Active_Desc");

    /// <summary>
    /// The connection was explicitly revoked by the member.
    /// </summary>
    public static readonly ConnectionStatus Revoked = new("R", "REVOKED", "ConnectionStatus_Revoked_Desc");

    /// <summary>
    /// The connection expired or tokens failed to refresh.
    /// </summary>
    public static readonly ConnectionStatus Expired = new("E", "EXPIRED", "ConnectionStatus_Expired_Desc");

    /// <summary>
    /// Gets the unique short-hand identifier for this status (e.g., "A", "R", "E").
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the uppercase display name of this status.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the localization key used by the i18n service to fetch the translated description.
    /// </summary>
    public string I18NKey { get; }

    private ConnectionStatus(string id, string name, string descriptionI18nKey)
    {
        Id = id;
        Name = name;
        I18NKey = descriptionI18nKey;
    }

    /// <summary>
    /// Retrieves a <see cref="ConnectionStatus"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The short-hand identifier (e.g., "A").</param>
    /// <returns>The matching <see cref="ConnectionStatus"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid status.</exception>
    public static ConnectionStatus FromId(string id)
    {
        return All.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid ConnectionStatus identifier.");
    }

    /// <summary>
    /// Returns a collection of all available <see cref="ConnectionStatus"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<ConnectionStatus> All => [Active, Revoked, Expired];
}
