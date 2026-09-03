using Application.ExternalSync.Commands.RequestExternalActivitySync;
using Domain.Members;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adapters.Strava.Controllers;

/// <summary>
/// Handles Strava manual activity sync endpoints.
/// </summary>
/// <param name="sender">The MediatR sender.</param>
[ApiController]
[Route("v1/Strava")]
public sealed class StravaSyncController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    /// <summary>
    /// Triggers a manual incremental sync of Strava activities for the authenticated member.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>202 Accepted</c> after durable background work is queued.
    /// </returns>
    /// <response code="202">Sync was accepted for background processing.</response>
    /// <response code="401">If the JWT is missing or invalid.</response>
    [HttpPost("sync")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ManualSync(CancellationToken cancellationToken)
    {
        var identifyName = HttpContext.Items["IdentifyName"] as string;
        if (string.IsNullOrEmpty(identifyName))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(
            new RequestExternalActivitySyncCommand(identifyName, ExternalProvider.Strava.Id),
            cancellationToken);

        return Accepted(new
        {
            providerId = result.ProviderId,
            state = result.State,
            statusUrl = "/v1/members/me/external-connections/STRAVA/sync-status"
        });
    }
}
