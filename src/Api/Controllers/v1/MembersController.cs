using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Api.Requests;
using Application.Dtos;
using Application.Members.Commands.Create;
using Application.Members.Commands.Patch;
using Application.Members.Commands.Update;
using Application.Members.Queries.Details;
using Application.Members.Queries.List;
using Application.Members.Queries.ResolveId;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Controller for interacting with members using "me", a member ID, or an identity name.
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="createMemberCommandValidator">The validator for <see cref="CreateMemberCommand"/>.</param>
/// <param name="updateMemberCommandValidator">The validator for <see cref="UpdateMemberCommand"/>.</param>
/// <param name="patchMemberCommandValidator">The validator for <see cref="PatchMemberCommand"/>.</param>
/// <param name="memberDetailsQueryValidator">The validator for <see cref="MemberDetailsQuery"/>.</param>
/// <exception cref="ArgumentNullException">Thrown when sender is null.</exception>
[Authorize]
[ApiController]
[Route("v1/[controller]")]
public sealed class MembersController(
    ISender sender,
    IValidator<CreateMemberCommand> createMemberCommandValidator,
    IValidator<UpdateMemberCommand> updateMemberCommandValidator,
    IValidator<PatchMemberCommand> patchMemberCommandValidator,
    IValidator<MemberDetailsQuery> memberDetailsQueryValidator)
    : ControllerBase
{
    private readonly ISender _sender = sender
        ?? throw new ArgumentNullException(nameof(sender));

    private string? CurrentIdentifyName => HttpContext.Items.TryGetValue("IdentifyName", out var name) && name is string n ? n : null;

    private Guid? CurrentMemberId => HttpContext.Items["MemberId"] is Guid id ? id : null;

    /// <summary>
    /// Creates a new member.
    /// </summary>
    /// <param name="request">The request containing member creation details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing the newly created <see cref="MemberDto"/> on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="CreateMemberCommand"/> object.
    /// </remarks>
    /// <response code="201">Returns the newly created member's details.</response>
    /// <response code="400">If the command validation fails.</response>
    /// <response code="401">If the authenticated member session is unavailable.</response>
    /// <response code="404">If not in the development environment.</response>
    [HttpPost]
    [DevelopmentOnly]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<MemberDto>> Create([FromBody] CreateMemberRequest request, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<ActionResult<MemberDto>> Core()
        {
            if (string.IsNullOrEmpty(CurrentIdentifyName))
            {
                return Unauthorized();
            }

            var command = new CreateMemberCommand(
                CurrentIdentifyName,
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.Description,
                request.PreferredTimezone,
                request.AutoLinkUserAdapterToPlan);

            return await CreateInternalAsync(command, token);
        }
    }

    private async Task<ActionResult<MemberDto>> CreateInternalAsync(CreateMemberCommand command, CancellationToken token)
    {
        await createMemberCommandValidator.ValidateAndThrowAsync(command, token);
        var newMemberGuid = await _sender.Send(command, token);

        var query = new MemberDetailsQuery(newMemberGuid);
        var memberDto = await _sender.Send(query, token);

        return CreatedAtAction(nameof(Read), new { identifier = newMemberGuid }, memberDto);
    }

    /// <summary>
    /// Updates an existing member.
    /// </summary>
    /// <param name="identifier">"me", the member's GUID, or their exact stored identity name.</param>
    /// <param name="request">The request containing member update details.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success, otherwise an appropriate error code.</returns>
    /// <remarks>
    /// The request body should contain a valid <see cref="UpdateMemberCommand"/> object.
    /// </remarks>
    /// <response code="204">If the member is updated successfully.</response>
    /// <response code="400">If the request body is null or command validation fails.</response>
    /// <response code="401">If the authenticated member session is unavailable.</response>
    /// <response code="403">If attempting to update another member.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpPut("{identifier}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(
        [FromRoute] string identifier,
        [FromBody] UpdateMemberRequest request,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<IActionResult> Core()
        {
            var id = await ResolveMemberIdAsync(identifier, token);
            if (id is null)
            {
                return Unauthorized();
            }

            if (id.Value != CurrentMemberId)
            {
                return Forbid();
            }

            var command = new UpdateMemberCommand(
                id.Value,
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.Description,
                request.PathAvatar,
                request.PreferredTimezone,
                request.AutoLinkUserAdapterToPlan);

            await updateMemberCommandValidator.ValidateAndThrowAsync(command, token);
            await _sender.Send(command, token);
            return NoContent();
        }
    }

    /// <summary>
    /// Partially updates an existing member (Field Mask pattern).
    /// </summary>
    /// <param name="identifier">"me", the member's GUID, or their exact stored identity name.</param>
    /// <param name="request">The request containing fields to update and the UpdateMask.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult with NoContent status code on success, otherwise an appropriate error code.</returns>
    /// <response code="204">If the requested fields are updated successfully.</response>
    /// <response code="400">If command validation fails.</response>
    /// <response code="401">If the authenticated member session is unavailable.</response>
    /// <response code="403">If attempting to patch another member.</response>
    /// <response code="404">If the identity name is not found.</response>
    [HttpPatch("{identifier}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Patch(
        [FromRoute] string identifier,
        [FromBody] PatchMemberRequest request,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Core();

        async Task<IActionResult> Core()
        {
            var id = await ResolveMemberIdAsync(identifier, token);
            if (id is null)
            {
                return Unauthorized();
            }

            if (id.Value != CurrentMemberId)
            {
                return Forbid();
            }

            var command = new PatchMemberCommand(
                id.Value,
                request.UpdateMask,
                request.FirstName,
                request.LastName,
                request.AutoLinkUserAdapterToPlan);

            await patchMemberCommandValidator.ValidateAndThrowAsync(command, token);
            await _sender.Send(command, token);
            return NoContent();
        }
    }

    /// <summary>
    /// Gets the details of a member.
    /// </summary>
    /// <param name="identifier">"me", the member's GUID, or their exact stored identity name.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An IActionResult containing a <see cref="MemberDto"/> object with member details on success, otherwise an appropriate error code.</returns>
    /// <response code="200">Returns a <see cref="MemberDto"/> object containing member details.</response>
    /// <response code="400">If query validation fails.</response>
    /// <response code="401">If the authenticated member session is unavailable.</response>
    /// <response code="404">If the member with the specified ID is not found.</response>
    [HttpGet("{identifier}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> Read([FromRoute] string identifier, CancellationToken token)
    {
        var id = await ResolveMemberIdAsync(identifier, token);
        if (id is null)
        {
            return Unauthorized();
        }

        var query = new MemberDetailsQuery(id.Value);
        await memberDetailsQueryValidator.ValidateAndThrowAsync(query, token);
        var memberDto = await _sender.Send(query, token);
        return Ok(memberDto);
    }

    private async Task<Guid?> ResolveMemberIdAsync(string identifier, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        if (identifier.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            return CurrentMemberId;
        }

        if (Guid.TryParse(identifier, out var id))
        {
            return id;
        }

        return await _sender.Send(new ResolveMemberIdQuery(identifier), token);
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
