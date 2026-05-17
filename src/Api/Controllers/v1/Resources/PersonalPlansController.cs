using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Application.Dtos;
using Application.Members.Commands.CreatePersonalPlan;
using Application.Members.Commands.UpdatePersonalPlan;
using Application.Members.Queries.ListPersonalPlans;
using Application.Members.Queries.PersonalPlanDetails;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1.Resources;

/// <summary>
/// Controller for manipulating Personal Plans using a flexible /me or member identifier pattern.
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="createPlanCommandValidator">The validator for <see cref="CreatePlanCommand"/>.</param>
/// <param name="updatePlanCommandValidator">The validator for <see cref="UpdatePlanCommand"/>.</param>
/// <param name="personalPlansQueryValidator">The validator for <see cref="ListPersonalPlansQuery"/>.</param>
/// <param name="personalPlanDetailsQueryValidator">The validator for <see cref="PersonalPlanDetailsQuery"/>.</param>
[Authorize]
[ServiceFilter(typeof(MemberSessionFilter))]
[ApiController]
[Route("v1/members/{identifier}/[controller]")]
public class PersonalPlansController(
    ISender sender,
    IValidator<CreatePlanCommand> createPlanCommandValidator,
    IValidator<UpdatePlanCommand> updatePlanCommandValidator,
    IValidator<ListPersonalPlansQuery> personalPlansQueryValidator,
    IValidator<PersonalPlanDetailsQuery> personalPlanDetailsQueryValidator)
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
    /// <param name="command">The command containing plan creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created personal plan's ID on success.</returns>
    /// <response code="200">Returns the newly created personal plan's ID.</response>
    /// <response code="400">If the command validation fails.</response>
    /// <response code="403">If attempting to create a plan for another user.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> Create(
        [FromRoute] string identifier,
        [FromBody] CreatePlanCommand command,
        CancellationToken token)
    {
        var targetIdentifyName = ResolveIdentifier(identifier);
        if (targetIdentifyName != CurrentUserIdentifyName)
        {
            return Forbid();
        }

        var createPlanCommand = command with { IdentifyName = targetIdentifyName! };
        await createPlanCommandValidator.ValidateAndThrowAsync(createPlanCommand, token);
        var newPlanGuid = await _sender.Send(createPlanCommand, token);
        return Ok(newPlanGuid);
    }

    /// <summary>
    /// Updates an existing personal plan.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="planId">The ID of the plan to update.</param>
    /// <param name="command">The command containing plan update details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success.</returns>
    /// <response code="204">If the member is updated successfully.</response>
    /// <response code="400">If the request body is null or command validation fails.</response>
    /// <response code="403">If attempting to update another user's plan.</response>
    [HttpPut("{planId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        [FromRoute] string identifier,
        [FromRoute] Guid planId,
        [FromBody] UpdatePlanCommand command,
        CancellationToken token)
    {
        var targetIdentifyName = ResolveIdentifier(identifier);
        if (targetIdentifyName != CurrentUserIdentifyName)
        {
            return Forbid();
        }

        var updatePlanCommand = command with { IdentifyName = targetIdentifyName!, PlanId = planId };
        await updatePlanCommandValidator.ValidateAndThrowAsync(updatePlanCommand, token);
        await _sender.Send(updatePlanCommand, token);
        return NoContent();
    }

    /// <summary>
    /// Gets all personal plans of a member.
    /// </summary>
    /// <param name="identifier">The identifier of the member ("me" or their identity name).</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A collection of personal plans.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonalPlanDto>>> Read(
        [FromRoute] string identifier,
        CancellationToken token)
    {
        var targetIdentifyName = ResolveIdentifier(identifier);
        var query = new ListPersonalPlansQuery(targetIdentifyName!);
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
        var query = new PersonalPlanDetailsQuery(targetIdentifyName!, planId);
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
}
