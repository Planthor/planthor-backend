using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Dtos;
using Backend.Application.Members.Commands.CreatePersonalPlan;
using Backend.Application.Members.Commands.UpdatePersonalPlan;
using Backend.Application.Members.Queries.ListPersonalPlans;
using Backend.Application.Members.Queries.PersonalPlanDetails;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.v1.Resources;

/// <summary>
/// Controller for manipulating Personal Plans.
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="createPlanCommandValidator">The validator for <see cref="CreatePlanCommand"/>.</param>
/// <param name="updatePlanCommandValidator">The validator for <see cref="UpdatePlanCommand"/>.</param>
/// <param name="personalPlansQueryValidator">The validator for <see cref="ListPersonalPlansQuery"/>.</param>
/// <param name="personalPlanDetailsQueryValidator">The validator for <see cref="PersonalPlanDetailsQuery"/>.</param>
[ApiController]
[Route("members/{memberId}/[controller]")]
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
    /// Create a new personal plan
    /// </summary>
    /// <param name="memberId">The ID of the member to create plan.</param>
    /// <param name="command">The command containing plan creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created personal plan's ID on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="CreatePlanCommand"/> object.
    /// </remarks>
    /// <response code="200">Returns the newly created personal plan's ID.</response>
    /// <response code="400">If the command validation fails.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(
        [FromRoute] Guid memberId,
        [FromBody] CreatePlanCommand command,
        CancellationToken token)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var createPlanCommand = command with { MemberId = memberId };
        await createPlanCommandValidator.ValidateAndThrowAsync(createPlanCommand, token);
        var newPlanGuid = await _sender.Send(command, token);
        return Ok(newPlanGuid);
    }

    /// <summary>
    /// Updates an existing personal plan.
    /// </summary>
    /// <param name="memberId">The ID of the member that owns the plan.</param>
    /// <param name="planId">The ID of the plan to update.</param>
    /// <param name="command">The command containing plan update details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="UpdatePlanCommand"/> object.
    /// </remarks>
    /// <response code="204">If the member is updated successfully.</response>
    /// <response code="400">If the request body is null or command validation fails.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpPut("{planId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid memberId,
        [FromRoute] Guid planId,
        [FromBody] UpdatePlanCommand command,
        CancellationToken token)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var updatePlanCommand = command with { MemberId = memberId, PlanId = planId };
        await updatePlanCommandValidator.ValidateAndThrowAsync(updatePlanCommand, token);
        await _sender.Send(updatePlanCommand, token);
        return NoContent();
    }

    /// <summary>
    /// Gets the all personal plans of a member.
    /// </summary>
    /// <param name="memberId">The ID of the member that owns plans.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonalPlanDto>>> Read(
        [FromRoute] Guid memberId,
        CancellationToken token)
    {
        var query = new ListPersonalPlansQuery(memberId);
        await personalPlansQueryValidator.ValidateAndThrowAsync(query, token);
        var personalPlanDtos = await _sender.Send(query, token);
        return Ok(personalPlanDtos);
    }

    /// <summary>
    /// Gets the details of a personal plan.
    /// </summary>
    /// <param name="memberId">The ID of the member that owns the plan.</param>
    /// <param name="planId">The ID of the personal plan to retrieve.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing a <see cref="PersonalPlanDto"/> object with member details on success, otherwise an appropriate error code.</returns>
    /// <response code="200">Returns a <see cref="PersonalPlanDto"/> object containing plan details.</response>
    /// <response code="400">If query validation fails.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpGet("{planId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalPlanDto>> Read(
        [FromRoute] Guid memberId,
        [FromRoute] Guid planId,
        CancellationToken token)
    {
        var query = new PersonalPlanDetailsQuery(memberId, planId);
        await personalPlanDetailsQueryValidator.ValidateAndThrowAsync(query, token);
        var personalPlanDto = await _sender.Send(query, token);
        return Ok(personalPlanDto);
    }

    /// <summary>
    /// NOT IMPLEMENTED YET.
    /// Preserved for updated custom Personal Plans Ordering, bulk plan updates.
    /// </summary>
    /// <param name="memberId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">Not yet implemented.</exception>
    /// <response code="500">Not yet implemented.</response>
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Patch(Guid memberId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Preserved for updated custom Personal Plans Ordering, bulk plan updates");
    }
}
