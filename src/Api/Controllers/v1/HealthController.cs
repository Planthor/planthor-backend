using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Public health check endpoint for verifying API connectivity and readiness.
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Returns the current health status of the API.
    /// </summary>
    /// <returns>A JSON object containing status, timestamp, and version.</returns>
    /// <response code="200">The API is healthy and responding.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
            version = "1.0"
        });
    }
}
