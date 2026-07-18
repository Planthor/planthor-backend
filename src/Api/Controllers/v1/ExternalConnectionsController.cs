using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Members.Queries.ExternalConnections.Details;
using Application.Members.Queries.ExternalConnections.List;
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
[Authorize]
[ApiController]
[Route("v1/members/{identifier}/[controller]")]
public class ExternalConnectionsController(
    ISender sender,
    IValidator<ListExternalConnectionsQuery> listQueryValidator,
    IValidator<ExternalConnectionDetailsQuery> detailsQueryValidator) : ControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Gets all external connections for a member.
    /// </summary>
    /// <param name="identifier">The member identifier, which can be '@me' or a valid GUID.</param>
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
        var identifyName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identifyName))
        {
            return Unauthorized();
        }

        var query = new ListExternalConnectionsQuery(identifier, identifyName);
        await listQueryValidator.ValidateAndThrowAsync(query, token);
        
        var result = await _sender.Send(query, token);
        return Ok(result);
    }

    /// <summary>
    /// Gets the details of a specific external connection.
    /// </summary>
    /// <param name="identifier">The member identifier, which can be '@me' or a valid GUID.</param>
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
        var identifyName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identifyName))
        {
            return Unauthorized();
        }

        var query = new ExternalConnectionDetailsQuery(identifier, identifyName, id);
        await detailsQueryValidator.ValidateAndThrowAsync(query, token);
        
        var result = await _sender.Send(query, token);
        return Ok(result);
    }
}
