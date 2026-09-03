using System.Collections.Generic;

namespace Api.Requests;

/// <summary>
/// Request model for sport-specific plan details.
/// </summary>
/// <remarks>
/// Use identifiers returned by <c>GET /v1/sport-types</c>. At least one identifier
/// is required, and <c>ALL</c> cannot be combined with another identifier.
/// </remarks>
/// <param name="SportTypes">
/// The canonical Planthor sport-type identifiers associated with the plan,
/// such as <c>RUN</c> or <c>RIDE</c>.
/// </param>
public record CreateSportPlanDetailsRequest(IReadOnlyList<string> SportTypes) : CreatePlanDetailsRequest;
