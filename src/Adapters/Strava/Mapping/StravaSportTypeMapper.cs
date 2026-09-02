using Domain.Plans;

namespace Adapters.Strava.Mapping;

/// <summary>
/// Maps Strava sport_type values to canonical Planthor sport types.
/// </summary>
public sealed class StravaSportTypeMapper
{
    private static readonly Dictionary<string, PlanthorSportType> Mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Run", PlanthorSportType.Run },
        { "TrailRun", PlanthorSportType.Run },
        { "VirtualRun", PlanthorSportType.Run },
        
        { "Walk", PlanthorSportType.Walk },
        
        { "Hike", PlanthorSportType.Hike },
        
        { "Ride", PlanthorSportType.Ride },
        { "MountainBikeRide", PlanthorSportType.Ride },
        { "GravelRide", PlanthorSportType.Ride },
        { "EBikeRide", PlanthorSportType.Ride },
        { "EMountainBikeRide", PlanthorSportType.Ride },
        { "VirtualRide", PlanthorSportType.Ride },
        { "Velomobile", PlanthorSportType.Ride },
        { "Handcycle", PlanthorSportType.Ride },
        { "Wheelchair", PlanthorSportType.Ride },
        
        { "Swim", PlanthorSportType.Swim }
    };

    /// <summary>
    /// Maps a Strava sport type to a canonical Planthor sport identifier.
    /// </summary>
    /// <param name="providerSportType">The Strava <c>sport_type</c> value.</param>
    /// <returns>The canonical identifier, or <c>null</c> when unsupported.</returns>
    public static string? MapToCanonicalId(string providerSportType)
    {
        if (string.IsNullOrWhiteSpace(providerSportType))
        {
            return null;
        }

        return Mappings.TryGetValue(providerSportType, out var planthorSportType)
            ? planthorSportType.Id
            : null;
    }
}
