using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanthorWebApi.Application.Dtos;
using PlanthorWebApi.Application.Members.Commands.Create;
using PlanthorWebApi.Application.Members.Commands.Update;
using PlanthorWebApi.Application.Members.Queries.Details;
using PlanthorWebApi.Application.Members.Queries.Lists;

namespace PlanthorWebApi.Api.Controllers;

/// <summary>
/// Controller for manipulating Members.
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="createMemberCommandValidator">The validator for <see cref="CreateMemberCommand"/>.</param>
/// <param name="updateMemberCommandValidator">The validator for <see cref="UpdateMemberCommand"/>.</param>
/// <param name="memberDetailsQueryValidator">The validator for <see cref="MemberDetailsQuery"/>.</param>
/// <exception cref="ArgumentNullException">Thrown when sender is null.</exception>
[ApiController]
[Route("[controller]")]
public class MembersController(
    ISender sender,
    IValidator<CreateMemberCommand> createMemberCommandValidator,
    IValidator<UpdateMemberCommand> updateMemberCommandValidator,
    IValidator<MemberDetailsQuery> memberDetailsQueryValidator)
    : ControllerBase
{
    private readonly ISender _sender = sender
        ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Creates a new member.
    /// </summary>
    /// <param name="command">The command containing member creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created member's ID on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="CreateMemberCommand"/> object.
    /// </remarks>
    /// <response code="200">Returns the newly created member's ID.</response>
    /// <response code="400">If the command validation fails.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateMemberCommand command, CancellationToken token)
    {
        await createMemberCommandValidator.ValidateAndThrowAsync(command, token);
        var newMemberGuid = await _sender.Send(command, token);
        return Ok(newMemberGuid);
    }

    /// <summary>
    /// Updates an existing member.
    /// </summary>
    /// <param name="id">The ID of the member to update.</param>
    /// <param name="command">The command containing member update details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="UpdateMemberCommand"/> object.
    /// </remarks>
    /// <response code="204">If the member is updated successfully.</response>
    /// <response code="400">If the request body is null or command validation fails.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateMemberCommand command,
        CancellationToken token)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var updateMemberCommand = command with { Id = id };
        await updateMemberCommandValidator.ValidateAndThrowAsync(updateMemberCommand, token);
        await _sender.Send(updateMemberCommand, token);
        return NoContent();
    }

    /// <summary>
    /// Gets the details of a member.
    /// </summary>
    /// <param name="id">The ID of the member to retrieve.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing a <see cref="MemberDto"/> object with member details on success, otherwise an appropriate error code.</returns>
    /// <response code="200">Returns a <see cref="MemberDto"/> object containing member details.</response>
    /// <response code="400">If query validation fails.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> Read(Guid id, CancellationToken token)
    {
        var query = new MemberDetailsQuery(id);
        await memberDetailsQueryValidator.ValidateAndThrowAsync(query, token);
        var memberDto = await _sender.Send(query, token);
        return Ok(memberDto);
    }

    /// <summary>
    /// Gets all members.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing a <see cref="MemberDto"/> object with member details on success, otherwise an appropriate error code.</returns>
    /// <response code="200">Returns a <see cref="MemberDto"/> object containing member details.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MemberDto>>> Read(CancellationToken token)
    {
        var query = new ListMembersQuery();
        return Ok(await _sender.Send(query, token));
    }
}
