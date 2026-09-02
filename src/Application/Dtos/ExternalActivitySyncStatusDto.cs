using System;

namespace Application.Dtos;

/// <summary>
/// Represents the operational synchronization state exposed to an owning member.
/// </summary>
/// <param name="ProviderId">The external provider identifier.</param>
/// <param name="InitialSyncState">The initial historical-sync state.</param>
/// <param name="State">The current synchronization state.</param>
/// <param name="LastTrigger">The most recent trigger kind.</param>
/// <param name="LastStartedAt">When the most recent run started.</param>
/// <param name="LastSuccessfulSyncAt">When a run most recently completed successfully.</param>
/// <param name="NextAttemptAt">When deferred work may resume.</param>
/// <param name="ErrorCode">A stable machine-readable error code.</param>
public sealed record ExternalActivitySyncStatusDto(
    string ProviderId,
    string InitialSyncState,
    string State,
    string? LastTrigger,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastSuccessfulSyncAt,
    DateTimeOffset? NextAttemptAt,
    string? ErrorCode);
