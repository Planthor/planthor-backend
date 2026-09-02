using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.ExternalSync.Queries.GetExternalActivitySyncStatus;
using Application.Members.Commands.DisconnectExternalProvider;
using Application.Members.Queries.ExternalConnections.Details;
using Application.Members.Queries.ExternalConnections.List;
using Domain.Members;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

/// <summary>
/// Controller for interacting with member's external connections.
/// </summary>
/// <param name="sender">The mediator used to send commands and queries.</param>
/// <param name="listQueryValidator">The validator for <see cref="ListExternalConnectionsQuery"/>.</param>
/// <param name="detailsQueryValidator">The validator for <see cref="ExternalConnectionDetailsQuery"/>.</param>
/// <param name="syncStatusQueryValidator">The validator for activity synchronization status queries.</param>
[Authorize]
[ApiController]
[Route("v1/members/{identifier}/[controller]")]
public sealed class ExternalConnectionsController(
    ISender sender,
    IValidator<ListExternalConnectionsQuery> listQueryValidator,
    IValidator<ExternalConnectionDetailsQuery> detailsQueryValidator,
    IValidator<GetExternalActivitySyncStatusQuery> syncStatusQueryValidator) : ControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    private string? CurrentIdentifyName => HttpContext.Items.TryGetValue("IdentifyName", out var name) && name is string n ? n : null;

    /// <summary>
    /// Gets all external connections for a member.
    /// </summary>
    /// <param name="identifier">The member identifier, which can be 'me' or a valid GUID.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A list of external connections.</returns>
    /// <response code="200">Returns the list of external connections.</response>
    /// <response code="400">If query validation fails.</response>
    /// <response code="401">If the user is unauthorized.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ExternalConnectionDto>>> ReadAll(string identifier, CancellationToken token)
    {
        if (string.IsNullOrEmpty(CurrentIdentifyName))
        {
            return Unauthorized();
        }

        var query = new ListExternalConnectionsQuery(identifier, CurrentIdentifyName);
        await listQueryValidator.ValidateAndThrowAsync(query, token);
        
        var result = await _sender.Send(query, token);
        return Ok(result);
    }

    /// <summary>
    /// Gets the details of a specific external connection.
    /// </summary>
    /// <param name="identifier">The member identifier, which can be 'me' or a valid GUID.</param>
    /// <param name="id">The unique identifier of the external connection.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The external connection details.</returns>
    /// <response code="200">Returns the external connection details.</response>
    /// <response code="400">If query validation fails.</response>
    /// <response code="401">If the user is unauthorized.</response>
    /// <response code="404">If the connection or member is not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExternalConnectionDto>> Read(string identifier, Guid id, CancellationToken token)
    {
        if (string.IsNullOrEmpty(CurrentIdentifyName))
        {
            return Unauthorized();
        }

        var query = new ExternalConnectionDetailsQuery(identifier, CurrentIdentifyName, id);
        await detailsQueryValidator.ValidateAndThrowAsync(query, token);
        
        var result = await _sender.Send(query, token);
        return Ok(result);
    }

    /// <summary>
    /// Disconnects a specific external connection.
    /// </summary>
    /// <param name="identifier">The member identifier, which can be 'me' or a valid GUID.</param>
    /// <param name="providerId">The unique identifier of the external provider (e.g., 'STRAVA').</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">The connection was successfully disconnected.</response>
    /// <response code="400">If command validation fails.</response>
    /// <response code="401">If the user is unauthorized.</response>
    /// <response code="404">If the connection or member is not found.</response>
    [HttpDelete("{providerId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disconnect(string identifier, string providerId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(CurrentIdentifyName))
        {
            return Unauthorized();
        }

        // We hardcode ActivitiesSync for now as it's the only supported type. 
        // In the future, this could be passed as a route or query parameter.
        var command = new DisconnectExternalProviderCommand(
            CurrentIdentifyName, 
            providerId, 
            ExternalConnectionType.ActivitiesSync.Id);

        await _sender.Send(command, token);
        return NoContent();
    }

    /// <summary>Gets the current activity synchronization status for an owned external connection.</summary>
    /// <param name="identifier">The member identifier, normally <c>me</c>.</param>
    /// <param name="providerId">The external provider identifier.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The provider-neutral synchronization status.</returns>
    /// <response code="200">Returns the current operational status.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="404">The connection is absent or not owned by the caller.</response>
    [HttpGet("{providerId}/sync-status")]
    [ProducesResponseType(typeof(ExternalActivitySyncStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExternalActivitySyncStatusDto>> ReadSyncStatus(
        string identifier,
        string providerId,
        CancellationToken token)
    {
        if (string.IsNullOrEmpty(CurrentIdentifyName))
        {
            return Unauthorized();
        }

        var query = new GetExternalActivitySyncStatusQuery(
            identifier,
            CurrentIdentifyName,
            providerId);
        await syncStatusQueryValidator.ValidateAndThrowAsync(query, token);
        return Ok(await _sender.Send(query, token));
    }
}
