using NodaTime;

namespace Adapters.Strava.Client;

/// <summary>
/// Typed result returned by the Strava HTTP boundary.
/// </summary>
/// <typeparam name="T">The successful response value type.</typeparam>
/// <param name="Outcome">The typed outcome indicating success or the specific type of failure.</param>
/// <param name="Value">The successful response value, if the outcome is <see cref="StravaApiOutcome.Success"/>.</param>
/// <param name="RetryAt">The earliest safe retry instant, useful for rate limits or transient failures.</param>
/// <param name="ErrorCode">A stable adapter error code.</param>
public sealed record StravaApiResult<T>(
    StravaApiOutcome Outcome,
    T? Value = default,
    Instant? RetryAt = null,
    string? ErrorCode = null);
