using NodaTime;

namespace Application.Shared;

/// <summary>
/// Primitive job payload used to schedule provider-neutral external activity work.
/// </summary>
/// <param name="ProviderId">The external provider identifier.</param>
/// <param name="ExternalUserId">The athlete identifier assigned by the provider.</param>
/// <param name="Trigger">The trigger kind: initial, manual, webhook, or retry.</param>
/// <param name="IdempotencyKey">A stable key used to coalesce duplicate triggers.</param>
/// <param name="ExternalActivityId">The optional single activity referenced by a webhook.</param>
/// <param name="NotBefore">The earliest instant at which the trigger may fire.</param>
/// <param name="RetryCount">The number of prior infrastructure retries.</param>
public sealed record ExternalActivitySyncJobRequest(
    string ProviderId,
    string ExternalUserId,
    string Trigger,
    string IdempotencyKey,
    string? ExternalActivityId = null,
    Instant? NotBefore = null,
    int RetryCount = 0);
