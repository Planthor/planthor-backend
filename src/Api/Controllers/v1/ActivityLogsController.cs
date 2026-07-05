using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Controller for manipulating Activity Logs associated with a Plan.
/// TODO - Trung: Update details
/// </summary>
[Authorize]
[ServiceFilter(typeof(MemberSessionFilter))]
[ApiController]
[Route("v1/plans/{planId}/[controller]")]
public class ActivityLogsController(ISender sender)
    : ControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Gets the authenticated user's identity name from claims.
    /// </summary>
    private string? CurrentUserIdentifyName => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Creates a new activity log for a specific plan.
    /// </summary>
    /// <remarks>
    /// Note: Creating an Activity Log is typically handled by the Strava Adapter. 
    /// This endpoint is primarily preserved here for testing purposes.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Guid>> Create()
    {
        var identifyName = CurrentUserIdentifyName;
        if (identifyName == null)
        {
            return Unauthorized();
        }

        return Ok();
    }

    /// <summary>
    /// Updates an existing activity log.
    /// </summary>
    [HttpPut("{logId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid planId,
        [FromRoute] Guid logId,
        CancellationToken token)
    {
        var identifyName = CurrentUserIdentifyName;
        if (identifyName == null)
        {
            return Unauthorized();
        }

        return NoContent();
    }

    /// <summary>
    /// Gets all activity logs for a specific plan.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Read(
        [FromRoute] Guid planId,
        [FromQuery] int? limit,
        [FromQuery] Guid? cursor,
        CancellationToken token)
    {
        var identifyName = CurrentUserIdentifyName;
        if (identifyName == null)
        {
            return Unauthorized();
        }

        return Ok();
    }

    /// <summary>
    /// Gets the details of a specific activity log.
    /// </summary>
    [HttpGet("{logId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Read(
        [FromRoute] Guid planId,
        [FromRoute] Guid logId,
        CancellationToken token)
    {
        var identifyName = CurrentUserIdentifyName;
        if (identifyName == null)
        {
            return Unauthorized();
        }

        return Ok();
    }

    /// <summary>
    /// Deletes a specific activity log.
    /// </summary>
    [HttpDelete("{logId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid planId,
        [FromRoute] Guid logId,
        CancellationToken token)
    {
        var identifyName = CurrentUserIdentifyName;
        if (identifyName == null)
        {
            return Unauthorized();
        }

        return NoContent();
    }

    /// <summary>
    /// NOT IMPLEMENTED YET.
    /// Preserved for potential bulk updates.
    /// </summary>
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Patch(Guid planId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Preserved for potential bulk updates of activity logs.");
    }
}
