namespace Domain.Plans;

/// <summary>
/// Converts provider-neutral SI distance values into a Planthor plan's configured distance unit.
/// </summary>
public static class ActivityDistance
{
    private const double MetersPerKilometer = 1000d;
    private const double MetersPerMile = 1609.344d;
    private const double MetersPerYard = 0.9144d;

    /// <summary>
    /// Converts a positive distance in meters into a supported Planthor unit.
    /// </summary>
    /// <param name="meters">The source distance in meters.</param>
    /// <param name="unit">The plan unit: m, km, mi, or yd.</param>
    /// <returns>The converted value, or <c>null</c> for invalid input or unsupported units.</returns>
    public static float? ConvertMeters(double? meters, string unit)
    {
        if (meters is null or <= 0 || double.IsNaN(meters.Value) || double.IsInfinity(meters.Value))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        var value = unit.Trim().ToUpperInvariant() switch
        {
            "M" => meters.Value,
            "KM" => meters.Value / MetersPerKilometer,
            "MI" => meters.Value / MetersPerMile,
            "YD" => meters.Value / MetersPerYard,
            _ => double.NaN
        };

        return double.IsNaN(value) || value > float.MaxValue ? null : (float)value;
    }
}
