using System.Collections.Generic;
using System.Linq;
using Api.Responses;
using Domain.Plans;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Provides metadata about the canonical sport types supported by Planthor.
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public sealed class SportTypesController : ControllerBase
{
    /// <summary>
    /// Lists the canonical sport-type identifiers accepted by Planthor sport-plan APIs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Clients should use the returned <c>id</c> values in
    /// <c>planDetails.sportTypes</c>. The identifiers are returned in their canonical
    /// uppercase form and should be treated as stable machine-readable values.
    /// </para>
    /// <para>
    /// <c>ALL</c> must be supplied alone and represents every supported canonical
    /// sport type. External activity providers normalize their provider-specific
    /// values into these identifiers before Planthor evaluates a sport plan.
    /// Activities without a supported mapping are ignored during automatic synchronization.
    /// </para>
    /// <para>
    /// This endpoint returns Planthor identifiers, not provider-specific sport-type values.
    /// </para>
    /// </remarks>
    /// <returns>A collection containing the canonical sport-type catalog.</returns>
    [HttpGet]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(IEnumerable<SportTypeResponse>), StatusCodes.Status200OK)]
    public IActionResult List()
    {
        var sportTypes = PlanthorSportType.All_List
            .Select(st => new SportTypeResponse(st.Id, st.Name));
        return Ok(sportTypes);
    }
}
