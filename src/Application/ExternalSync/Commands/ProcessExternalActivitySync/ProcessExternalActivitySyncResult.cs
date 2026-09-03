using NodaTime;

namespace Application.ExternalSync.Commands.ProcessExternalActivitySync;

/// <summary>
/// Reports committed domain effects and any persistent retry requested by the adapter.
/// </summary>
/// <param name="LogsCreated">The number of new Plan activity logs committed.</param>
/// <param name="RetryAt">The earliest retry instant, or <c>null</c> for terminal work.</param>
/// <param name="ErrorCode">A stable machine-readable deferral or failure code.</param>
public sealed record ProcessExternalActivitySyncResult(
    int LogsCreated,
    Instant? RetryAt = null,
    string? ErrorCode = null);
