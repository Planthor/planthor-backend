using System.Collections.Generic;

namespace Api.Requests;

/// <summary>
/// Request model for sport-specific plan details.
/// </summary>
/// <param name="SportTypes">A list of sport types (e.g., "Run", "Ride") associated with the plan.</param>
public record CreateSportPlanDetailsRequest(IReadOnlyList<string> SportTypes) : CreatePlanDetailsRequest;
