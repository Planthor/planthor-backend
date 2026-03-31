using Backend.Adapters.Strava.Webhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Adapters.Strava.Controllers;

/// <summary>
/// Handles Strava-specific endpoints: webhook subscription verification,
/// real-time push event processing, and manual activity sync.
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public class StravaController() : ControllerBase
{
    /// <summary>
    /// Receives a real-time push event from Strava.
    /// </summary>
    /// <param name="payload">The Strava webhook event payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> always (Strava requires fast acknowledgement).</returns>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveEvent(
        [FromBody] StravaWebhookPayload payload,
        CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Triggers a manual incremental sync of Strava activities for the authenticated member.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>200 OK</c> with the number of new activity logs created.
    /// <c>401 Unauthorized</c> if the JWT is missing or invalid.
    /// </returns>
    [HttpPost("sync")]
    [Authorize]
    public async Task<IActionResult> ManualSync(CancellationToken ct)
    {
        throw new NotSupportedException();
    }
}
