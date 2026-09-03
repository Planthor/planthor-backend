using System.Globalization;
using Adapters.Strava.Configuration;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Adapters.Strava.Coordinator;

/// <summary>
/// Tracks Strava's application-wide read quota and reserves configured headroom for webhooks.
/// Ensures that background historical syncs do not exhaust the API limits needed for real-time operations.
/// </summary>
/// <param name="clock">The system clock used for time-based limit reset calculations.</param>
/// <param name="options">The Strava options containing the configured headroom percentage.</param>
public sealed class StravaRateLimitCoordinator(IClock clock, IOptions<StravaOptions> options)
{
    private const int DefaultQuarterHourLimit = 200;
    private const int DefaultDailyLimit = 2000;
    private static readonly Duration ResetJitter = Duration.FromSeconds(5);
    private readonly object _gate = new();
    private readonly int _headroomPercentage = Math.Clamp(
        options.Value.HistoricalQuotaHeadroomPercentage,
        0,
        90);
    private int _quarterHourUsage;
    private int _dailyUsage;
    private int _quarterHourLimit = DefaultQuarterHourLimit;
    private int _dailyLimit = DefaultDailyLimit;

    /// <summary>
    /// Evaluates current API usage against the configured headroom percentage to determine
    /// if historical/background work should yield its quota to prioritize real-time fetches.
    /// </summary>
    /// <remarks>
    /// A lock is required here to ensure thread-safe memory visibility when reading the usage and limit 
    /// fields, preventing race conditions from concurrent updates across multiple background sync tasks.
    /// </remarks>
    /// <returns>
    /// An <see cref="Instant"/> representing the next limit reset time if deferral is required, 
    /// or <c>null</c> if sufficient quota remains.
    /// </returns>
    public Instant? GetHistoricalDeferral()
    {
        lock (_gate)
        {
            var usableFraction = (100 - _headroomPercentage) / 100d;
            if (_dailyUsage >= Math.Floor(_dailyLimit * usableFraction))
            {
                return NextUtcMidnight();
            }

            return _quarterHourUsage >= Math.Floor(_quarterHourLimit * usableFraction)
                ? NextQuarterHour()
                : null;
        }
    }

    /// <summary>
    /// Extracts and tracks the latest usage and limit numbers by parsing the 
    /// <c>X-ReadRateLimit-Usage</c> (or <c>X-RateLimit-Usage</c>) headers from a Strava API response.
    /// </summary>
    /// <remarks>
    /// A lock is utilized to atomically update both the short-window and daily usage metrics, 
    /// ensuring that concurrent API responses from various threads do not interleave or corrupt the state.
    /// </remarks>
    /// <param name="response">The HTTP response returned by the Strava API.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided response is null.</exception>
    public void Observe(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var usage = ReadPair(response, "X-ReadRateLimit-Usage") ??
                    ReadPair(response, "X-RateLimit-Usage");
        var limits = ReadPair(response, "X-ReadRateLimit-Limit") ??
                     ReadPair(response, "X-RateLimit-Limit");

        lock (_gate)
        {
            if (usage is not null)
            {
                (_quarterHourUsage, _dailyUsage) = usage.Value;
            }

            if (limits is not null)
            {
                (_quarterHourLimit, _dailyLimit) = limits.Value;
            }
        }
    }

    /// <summary>
    /// Calculates the precise instant when the rate limit will reset after encountering 
    /// a <c>429 Too Many Requests</c> response.
    /// </summary>
    /// <remarks>
    /// Utilizes a lock to safely read the synchronized daily usage and limit state, 
    /// determining whether the 15-minute or daily quota was exhausted.
    /// </remarks>
    /// <returns>An <see cref="Instant"/> specifying when to retry, inclusive of a 5-second safety jitter.</returns>
    public Instant GetRetryAt()
    {
        lock (_gate)
        {
            return _dailyUsage >= _dailyLimit ? NextUtcMidnight() : NextQuarterHour();
        }
    }

    /// <summary>
    /// Calculates the instant of the next 15-minute window boundary, plus a small jitter.
    /// </summary>
    private Instant NextQuarterHour()
    {
        var nowEpoch = clock.GetCurrentInstant().ToUnixTimeSeconds();
        const long intervalSeconds = 15 * 60;
        return Instant.FromUnixTimeSeconds(((nowEpoch / intervalSeconds) + 1) * intervalSeconds)
            .Plus(ResetJitter);
    }

    /// <summary>
    /// Calculates the instant of the next UTC midnight, plus a small jitter.
    /// </summary>
    private Instant NextUtcMidnight()
    {
        var nextDate = clock.GetCurrentInstant().InUtc().Date.PlusDays(1);
        return nextDate.AtMidnight().InUtc().ToInstant().Plus(ResetJitter);
    }

    /// <summary>
    /// Helper method to extract the short-window and daily limit pairs from a specific header.
    /// </summary>
    private static (int ShortWindow, int Daily)? ReadPair(
        HttpResponseMessage response,
        string headerName)
    {
        if (!response.Headers.TryGetValues(headerName, out IEnumerable<string>? values))
        {
            return null;
        }

        foreach (var value in values)
        {
            var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var shortWindow) &&
                int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var daily))
            {
                return (shortWindow, daily);
            }
        }

        return null;
    }
}
