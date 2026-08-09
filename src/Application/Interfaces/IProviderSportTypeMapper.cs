using Domain.Plans;

namespace Application.Interfaces;

/// <summary>
/// Maps external provider sport type identifiers to canonical Planthor sport types.
/// </summary>
public interface IProviderSportTypeMapper
{
    /// <summary>
    /// Maps a provider-specific sport type string to a canonical Planthor sport type.
    /// Returns null if the provider type has no Planthor equivalent.
    /// </summary>
    /// <param name="providerSportType">The sport type string from the external provider.</param>
    /// <returns>The corresponding PlanthorSportType, or null if no mapping exists.</returns>
    PlanthorSportType? MapToPlanthor(string providerSportType);
}
