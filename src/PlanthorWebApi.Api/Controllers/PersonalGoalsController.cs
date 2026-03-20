using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanthorWebApi.Application.Dtos;
using PlanthorWebApi.Application.Members.Commands.CreateGoal;
using PlanthorWebApi.Application.Members.Commands.UpdateGoal;
using PlanthorWebApi.Application.Members.Queries.ListPersonalGoals;
using PlanthorWebApi.Application.Members.Queries.PersonalGoalDetailsQuery;

namespace PlanthorWebApi.Api.Controllers;

/// <summary>
/// Controller for manipulating Personal Goals.
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="createGoalCommandValidator">The validator for <see cref="CreateGoalCommand"/>.</param>
/// <param name="updateGoalCommandValidator">The validator for <see cref="UpdateGoalCommand"/>.</param>
/// <param name="personalGoalsQueryValidator">The validator for <see cref="ListPersonalGoalsQuery"/>.</param>
/// <param name="personalGoalDetailsQueryValidator">The validator for <see cref="PersonalGoalDetailsQuery"/>.</param>
[ApiController]
[Route("members/{memberId}/[controller]")]
public class PersonalGoalsController(
    ISender sender,
    IValidator<CreateGoalCommand> createGoalCommandValidator,
    IValidator<UpdateGoalCommand> updateGoalCommandValidator,
    IValidator<ListPersonalGoalsQuery> personalGoalsQueryValidator,
    IValidator<PersonalGoalDetailsQuery> personalGoalDetailsQueryValidator)
    : ControllerBase
{
    private readonly ISender _sender = sender
        ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Create a new personal goal
    /// </summary>
    /// <param name="memberId">The ID of the member to create goal.</param>
    /// <param name="command">The command containing goal creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created personal goal's ID on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="CreateGoalCommand"/> object.
    /// </remarks>
    /// <response code="200">Returns the newly created personal goal's ID.</response>
    /// <response code="400">If the command validation fails.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(
        [FromRoute] Guid memberId,
        [FromBody] CreateGoalCommand command,
        CancellationToken token)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var createGoalCommand = command with { MemberId = memberId };
        await createGoalCommandValidator.ValidateAndThrowAsync(createGoalCommand, token);
        var newGoalGuid = await _sender.Send(command, token);
        return Ok(newGoalGuid);
    }

    /// <summary>
    /// Updates an existing personal goal.
    /// </summary>
    /// <param name="memberId">The ID of the member that owns the goal.</param>
    /// <param name="goalId">The ID of the goal to update.</param>
    /// <param name="command">The command containing goal update details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="UpdateGoalCommand"/> object.
    /// </remarks>
    /// <response code="204">If the member is updated successfully.</response>
    /// <response code="400">If the request body is null or command validation fails.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpPut("{goalId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid memberId,
        [FromRoute] Guid goalId,
        [FromBody] UpdateGoalCommand command,
        CancellationToken token)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var updateGoalCommand = command with { MemberId = memberId, GoalId = goalId };
        await updateGoalCommandValidator.ValidateAndThrowAsync(updateGoalCommand, token);
        await _sender.Send(updateGoalCommand, token);
        return NoContent();
    }

    /// <summary>
    /// Gets the all personal goals of a member.
    /// </summary>
    /// <param name="memberId">The ID of the member that owns goals.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonalGoalDto>>> Read(
        [FromRoute] Guid memberId,
        CancellationToken token)
    {
        var query = new ListPersonalGoalsQuery(memberId);
        await personalGoalsQueryValidator.ValidateAndThrowAsync(query, token);
        var personalGoalDtos = await _sender.Send(query, token);
        return Ok(personalGoalDtos);
    }

    /// <summary>
    /// Gets the details of a personal goal.
    /// </summary>
    /// <param name="memberId">The ID of the member that owns the goal.</param>
    /// <param name="goalId">The ID of the personal goal to retrieve.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing a <see cref="PersonalGoalDto"/> object with member details on success, otherwise an appropriate error code.</returns>
    /// <response code="200">Returns a <see cref="PersonalGoalDto"/> object containing goal details.</response>
    /// <response code="400">If query validation fails.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpGet("{goalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalGoalDto>> Read(
        [FromRoute] Guid memberId,
        [FromRoute] Guid goalId,
        CancellationToken token)
    {
        var query = new PersonalGoalDetailsQuery(memberId, goalId);
        await personalGoalDetailsQueryValidator.ValidateAndThrowAsync(query, token);
        var personalGoalDto = await _sender.Send(query, token);
        return Ok(personalGoalDto);
    }

    /// <summary>
    /// NOT IMPLEMENTED YET.
    /// Preserved for updated custom Personal Goals Ordering, bulk goal updates.
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
        throw new NotImplementedException("Preserved for updated custom Personal Goals Ordering, bulk goal updates");
    }
}
