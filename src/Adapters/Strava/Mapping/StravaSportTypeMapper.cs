using Application.Interfaces;
using Domain.Plans;

namespace Adapters.Strava.Mapping;

/// <summary>
/// Implements <see cref="IProviderSportTypeMapper"/> for Strava.
/// Maps Strava sport_type values to canonical Planthor sport types.
/// </summary>
public sealed class StravaSportTypeMapper : IProviderSportTypeMapper
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

    /// <inheritdoc/>
    public PlanthorSportType? MapToPlanthor(string providerSportType)
    {
        if (string.IsNullOrWhiteSpace(providerSportType))
        {
            return null;
        }

        return Mappings.TryGetValue(providerSportType, out var planthorSportType)
            ? planthorSportType
            : null;
    }
}
