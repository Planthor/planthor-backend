using System.Collections.Generic;
using System.Linq;
using Api.Responses;
using Domain.Plans;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Provides metadata about the sport types supported by Planthor.
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public sealed class SportTypesController : ControllerBase
{
    /// <summary>
    /// Returns the list of all supported Planthor sport types.
    /// </summary>
    /// <returns>A collection of supported sport types.</returns>
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
