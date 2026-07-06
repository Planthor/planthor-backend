using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Application.Members.ActivityLogs.Commands.Create;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Controller for manipulating Activity Logs associated with a Plan.
/// TODO - Trung: Update details
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="createActivityLogCommandValidator">The validator for <see cref="CreateActivityLogCommand"/>.</param>
[Authorize]
[ServiceFilter(typeof(MemberSessionFilter))]
[ApiController]
[Route("v1/plans/{planId}/[controller]")]
public class ActivityLogsController(
    ISender sender,
    IValidator<CreateActivityLogCommand> createActivityLogCommandValidator)
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
    /// <param name="planId">The unique identifier of the plan to add the activity log to.</param>
    /// <param name="command">The command containing activity log creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created activity log's ID on success.</returns>
    /// <remarks>
    /// Note: Creating an Activity Log is typically handled by the Strava Adapter. 
    /// This endpoint is primarily preserved here for testing purposes.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> Create(
        [FromRoute] Guid planId,
        [FromBody] CreateActivityLogCommand command,
        CancellationToken token)
    {
        var identifyName = CurrentUserIdentifyName;
        if (string.IsNullOrEmpty(identifyName))
        {
            return Unauthorized();
        }

        var createLogCommand = command with { PlanId = planId, IdentifyName = identifyName };
        await createActivityLogCommandValidator.ValidateAndThrowAsync(createLogCommand, token);
        var newLogGuid = await _sender.Send(createLogCommand, token);
        return Ok(newLogGuid);
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
