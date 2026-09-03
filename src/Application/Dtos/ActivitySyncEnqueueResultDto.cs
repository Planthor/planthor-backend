namespace Application.Dtos;

/// <summary>
/// Acknowledges that external activity synchronization was accepted for background processing.
/// </summary>
/// <param name="ProviderId">The external provider identifier.</param>
/// <param name="State">The resulting operational state.</param>
public sealed record ActivitySyncEnqueueResultDto(string ProviderId, string State);
