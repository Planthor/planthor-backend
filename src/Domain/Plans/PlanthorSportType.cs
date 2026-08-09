using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Plans;

/// <summary>
/// Represents a supported sport type in the Planthor platform.
/// </summary>
public class PlanthorSportType
{
    /// <summary>
    /// Wildcard - accepts any Strava activity type. User does not care about specific sport types.
    /// </summary>
    public static readonly PlanthorSportType All = new("ALL", "All Sport Types", "SportType_All_Desc");

    /// <summary>
    /// Running activities.
    /// </summary>
    public static readonly PlanthorSportType Run = new("RUN", "Run", "SportType_Run_Desc");

    /// <summary>
    /// Walking activities.
    /// </summary>
    public static readonly PlanthorSportType Walk = new("WALK", "Walk", "SportType_Walk_Desc");

    /// <summary>
    /// Hiking activities.
    /// </summary>
    public static readonly PlanthorSportType Hike = new("HIKE", "Hike", "SportType_Hike_Desc");

    /// <summary>
    /// Cycling activities.
    /// </summary>
    public static readonly PlanthorSportType Ride = new("RIDE", "Ride", "SportType_Ride_Desc");

    /// <summary>
    /// Swimming activities.
    /// </summary>
    public static readonly PlanthorSportType Swim = new("SWIM", "Swim", "SportType_Swim_Desc");

    /// <summary>
    /// Gets the unique short-hand identifier for the sport type.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the sport type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the localization key used by the i18n service to fetch the translated description.
    /// </summary>
    public string I18NKey { get; }

    // Required by EF Core
    private PlanthorSportType() : this(default!, default!, default!) { }

    private PlanthorSportType(string id, string name, string i18nKey)
    {
        Id = id;
        Name = name;
        I18NKey = i18nKey;
    }

    /// <summary>
    /// Retrieves a <see cref="PlanthorSportType"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The identifier (e.g., "RUN").</param>
    /// <returns>The matching <see cref="PlanthorSportType"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided ID does not match any valid status.</exception>
    public static PlanthorSportType FromId(string id)
    {
        return All_List.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"'{id}' is not a valid PlanthorSportType identifier.");
    }

    /// <summary>
    /// Tries to retrieve a <see cref="PlanthorSportType"/> based on its unique string identifier.
    /// </summary>
    /// <param name="id">The identifier (e.g., "RUN").</param>
    /// <param name="result">The resulting <see cref="PlanthorSportType"/> instance, or null if not found.</param>
    /// <returns>True if a match was found, false otherwise.</returns>
    public static bool TryFromId(string id, out PlanthorSportType? result)
    {
        result = All_List.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return result is not null;
    }

    /// <summary>
    /// Returns a collection of all available <see cref="PlanthorSportType"/> definitions.
    /// </summary>
    public static IReadOnlyCollection<PlanthorSportType> All_List => [All, Run, Walk, Hike, Ride, Swim];
}
