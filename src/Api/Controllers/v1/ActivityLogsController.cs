using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Api.Requests;
using Application.Dtos;
using Application.Members.ActivityLogs.Commands.Create;
using Application.Members.ActivityLogs.Queries.Details;
using Domain.Members;
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
    /// <param name="request">The request body containing activity log creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created <see cref="ActivityLogDto"/> on success.</returns>
    /// <response code="201">Returns the newly created activity log's details.</response>
    /// <response code="400">If the command validation fails.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user does not have permission.</response>
    /// <remarks>
    /// Note: Creating an Activity Log is typically handled by the Strava Adapter. 
    /// This endpoint is primarily preserved here for testing purposes.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ActivityLogDto>> Create(
        [FromRoute] Guid planId,
        [FromBody] CreateActivityLogRequest request,
        CancellationToken token)
    {
        var identifyName = CurrentUserIdentifyName;
        if (string.IsNullOrEmpty(identifyName))
        {
            return Unauthorized();
        }

        ExternalActivitySource? externalSource = null;
        if (!string.IsNullOrEmpty(request.ExternalProviderId) && !string.IsNullOrEmpty(request.ExternalActivityId))
        {
            var provider = ExternalProvider.FromId(request.ExternalProviderId);
            externalSource = new ExternalActivitySource(provider, request.ExternalActivityId);
        }

        var createLogCommand = new CreateActivityLogCommand(
            PlanId: planId,
            Value: request.Value,
            ActivityLocalDate: request.ActivityLocalDate,
            IdentifyName: identifyName,
            ExternalSource: externalSource
        );

        await createActivityLogCommandValidator.ValidateAndThrowAsync(createLogCommand, token);
        var newLogGuid = await _sender.Send(createLogCommand, token);

        var query = new ActivityLogDetailsQuery(planId, newLogGuid);
        var activityLogDto = await _sender.Send(query, token);

        return CreatedAtAction(nameof(Read), new { planId = planId, logId = newLogGuid }, activityLogDto);
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
