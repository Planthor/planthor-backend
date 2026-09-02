using System.Collections.Generic;
using NodaTime;

namespace Application.Dtos;

/// <summary>
/// Provider-neutral result of fetching one activity or a bounded historical activity set.
/// </summary>
/// <param name="Outcome">The typed fetch outcome.</param>
/// <param name="Activities">Normalized activities when the operation succeeds.</param>
/// <param name="WatermarkCandidate">The historical upper bound to acknowledge after Plan writes commit.</param>
/// <param name="RetryAt">The earliest instant at which deferred work should resume.</param>
/// <param name="ErrorCode">A stable machine-readable failure code.</param>
public sealed record ActivitySyncFetchResultDto(
    ActivitySyncOutcome Outcome,
    IReadOnlyList<AdapterActivityDto> Activities,
    Instant? WatermarkCandidate = null,
    Instant? RetryAt = null,
    string? ErrorCode = null);
