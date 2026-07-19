using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Api.Requests;
using Application.Dtos;
using Application.Members.PersonalPlans.Commands.Create;
using Application.Members.PersonalPlans.Commands.Update;
using Application.Members.PersonalPlans.Commands.Cancel;
using Application.Members.PersonalPlans.Commands.Activate;
using Application.Members.PersonalPlans.Queries.Details;
using Application.Members.PersonalPlans.Queries.List;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Controller for manipulating Personal Plans using a flexible /me or member identifier pattern.
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="createPersonalPlanCommandValidator">The validator for <see cref="CreatePersonalPlanCommand"/>.</param>
/// <param name="updatePlanCommandValidator">The validator for <see cref="UpdatePersonalPlanCommand"/>.</param>
/// <param name="personalPlansQueryValidator">The validator for <see cref="ListPersonalPlansQuery"/>.</param>
/// <param name="personalPlanDetailsQueryValidator">The validator for <see cref="PersonalPlanDetailsQuery"/>.</param>
/// <param name="activatePlanCommandValidator">The validator for <see cref="ActivatePersonalPlanCommand"/>.</param>
[Authorize]
[ServiceFilter(typeof(MemberSessionFilter))]
[ApiController]
[Route("v1/members/{identifier}/[controller]")]
public class PersonalPlansController(
    ISender sender,
    IValidator<CreatePersonalPlanCommand> createPersonalPlanCommandValidator,
    IValidator<UpdatePersonalPlanCommand> updatePlanCommandValidator,
    IValidator<ListPersonalPlansQuery> personalPlansQueryValidator,
    IValidator<PersonalPlanDetailsQuery> personalPlanDetailsQueryValidator,
    IValidator<ActivatePersonalPlanCommand> activatePlanCommandValidator)
    : ControllerBase
{
    private readonly ISender _sender = sender
        ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Gets the authenticated user's identity name from claims.
    /// </summary>
    private string? CurrentUserIdentifyName => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Create a new personal plan.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="request">The request containing plan creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created <see cref="PersonalPlanDto"/> on success.</returns>
    /// <response code="201">Returns the newly created personal plan's details.</response>
    /// <response code="400">If the command validation fails.</response>
    /// <response code="403">If attempting to create a plan for another user.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PersonalPlanDto>> Create(
        [FromRoute] string identifier,
        [FromBody] CreatePersonalPlanRequest request,
        CancellationToken token)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var targetIdentifyName = ResolveIdentifier(identifier);

        if (string.IsNullOrEmpty(targetIdentifyName))
        {
            return Unauthorized();
        }

        if (targetIdentifyName != CurrentUserIdentifyName)
        {
            return Forbid();
        }

        var createPlanCommand = new CreatePersonalPlanCommand(
            targetIdentifyName,
            request.Name,
            request.Unit,
            request.Target,
            request.FromDate,
            request.ToDate,
            request.StartDateLocal,
            request.EndDateLocal,
            request.Timezone,
            request.EnableActivityLog,
            request.DisplayOnProfile,
            request.Prioritize,
            request.LinkUserAdapter);

        await createPersonalPlanCommandValidator.ValidateAndThrowAsync(createPlanCommand, token);
        var newPlanGuid = await _sender.Send(createPlanCommand, token);

        var query = new PersonalPlanDetailsQuery(targetIdentifyName, newPlanGuid);
        var personalPlanDto = await _sender.Send(query, token);

        return CreatedAtAction(nameof(Read), new { identifier = identifier, planId = newPlanGuid }, personalPlanDto);
    }

    /// <summary>
    /// Updates an existing personal plan.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="planId">The ID of the plan to update.</param>
    /// <param name="request">The request containing plan update details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success.</returns>
    /// <response code="204">If the member is updated successfully.</response>
    /// <response code="400">If the request body is null or command validation fails.</response>
    /// <response code="403">If attempting to update another user's plan.</response>
    [HttpPut("{planId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PersonalPlanDto>> Update(
        [FromRoute] string identifier,
        [FromRoute] Guid planId,
        [FromBody] UpdatePersonalPlanRequest request,
        CancellationToken token)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var targetIdentifyName = ResolveIdentifier(identifier);

        if (string.IsNullOrEmpty(targetIdentifyName))
        {
            return Unauthorized();
        }

        if (targetIdentifyName != CurrentUserIdentifyName)
        {
            return Forbid();
        }

        var updatePlanCommand = new UpdatePersonalPlanCommand(
            targetIdentifyName!,
            planId,
            request.Unit,
            request.Target,
            request.Current,
            request.FromDate,
            request.ToDate,
            request.PeriodType);

        await updatePlanCommandValidator.ValidateAndThrowAsync(updatePlanCommand, token);
        var updatedPlan = await _sender.Send(updatePlanCommand, token);
        return Ok(updatedPlan);
    }

    /// <summary>
    /// Gets all personal plans of a member.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="status">Optional status filters.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cursor">Pagination cursor (last seen plan ID).</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A collection of personal plans.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonalPlanDto>>> Read(
        [FromRoute] string identifier,
        [FromQuery] string[]? status,
        [FromQuery] int? limit,
        [FromQuery] Guid? cursor,
        CancellationToken token)
    {
        var targetIdentifyName = ResolveIdentifier(identifier);

        if (string.IsNullOrEmpty(targetIdentifyName))
        {
            return Unauthorized();
        }

        var query = new ListPersonalPlansQuery(
            IdentifyName: targetIdentifyName,
            Limit: limit ?? 10,
            Cursor: cursor,
            Statuses: status
        );

        await personalPlansQueryValidator.ValidateAndThrowAsync(query, token);
        var personalPlanDtos = await _sender.Send(query, token);
        return Ok(personalPlanDtos);
    }

    /// <summary>
    /// Gets the details of a personal plan.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="planId">The ID of the personal plan to retrieve.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="PersonalPlanDto"/> object with plan details.</returns>
    [HttpGet("{planId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalPlanDto>> Read(
        [FromRoute] string identifier,
        [FromRoute] Guid planId,
        CancellationToken token)
    {
        var targetIdentifyName = ResolveIdentifier(identifier);
        if (string.IsNullOrEmpty(targetIdentifyName))
        {
            return Unauthorized();
        }
        var query = new PersonalPlanDetailsQuery(targetIdentifyName, planId);
        await personalPlanDetailsQueryValidator.ValidateAndThrowAsync(query, token);
        var personalPlanDto = await _sender.Send(query, token);
        return Ok(personalPlanDto);
    }

    /// <summary>
    /// NOT IMPLEMENTED YET.
    /// Preserved for updated custom Personal Plans Ordering, bulk plan updates.
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>NotSupportedException.</returns>
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Patch(string identifier, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Preserved for updated custom Personal Plans Ordering, bulk plan updates");
    }

    private string? ResolveIdentifier(string identifier)
    {
        return identifier.Equals("me", StringComparison.OrdinalIgnoreCase)
            ? CurrentUserIdentifyName
            : identifier;
    }

    /// <summary>
    /// Cancels a personal plan.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="planId">The ID of the plan to cancel.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success.</returns>
    /// <response code="204">If the plan is cancelled successfully.</response>
    /// <response code="403">If attempting to cancel another user's plan.</response>
    /// <response code="404">If the plan is not found.</response>
    [HttpPost("{planId}:cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalPlanDto>> Cancel(
        [FromRoute] string identifier,
        [FromRoute] Guid planId,
        CancellationToken token)
    {
        var targetIdentifyName = ResolveIdentifier(identifier);

        if (string.IsNullOrEmpty(targetIdentifyName))
        {
            return Unauthorized();
        }

        if (targetIdentifyName != CurrentUserIdentifyName)
        {
            return Forbid();
        }

        var cancelPlanCommand = new CancelPlanCommand(targetIdentifyName, planId);
        var cancelledPlan = await _sender.Send(cancelPlanCommand, token);

        return Ok(cancelledPlan);
    }

    /// <summary>
    /// Activates a personal plan.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="planId">The ID of the plan to activate.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with Ok status code on success.</returns>
    /// <response code="200">If the plan is activated successfully.</response>
    /// <response code="403">If attempting to activate another user's plan.</response>
    /// <response code="404">If the plan is not found.</response>
    [HttpPost("{planId}:activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalPlanDto>> Activate(
        [FromRoute] string identifier,
        [FromRoute] Guid planId,
        CancellationToken token)
    {
        var targetIdentifyName = ResolveIdentifier(identifier);

        if (string.IsNullOrEmpty(targetIdentifyName))
        {
            return Unauthorized();
        }

        if (targetIdentifyName != CurrentUserIdentifyName)
        {
            return Forbid();
        }

        var activatePlanCommand = new ActivatePersonalPlanCommand(targetIdentifyName, planId);
        await activatePlanCommandValidator.ValidateAndThrowAsync(activatePlanCommand, token);
        var activatedPlan = await _sender.Send(activatePlanCommand, token);

        return Ok(activatedPlan);
    }
}
