using System.Globalization;
using System.Text.Json;
using Adapters.Strava.Configuration;
using Adapters.Strava.Webhook;
using Application.ExternalSync.Commands.EnqueueExternalActivitySync;
using Application.ExternalSync.Commands.EnqueueExternalConnectionRevocation;
using Application.Shared;
using Domain.Members;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adapters.Strava.Controllers;

/// <summary>
/// Handles Strava webhook subscription verification and real-time push event processing.
/// </summary>
/// <param name="options">The Strava options.</param>
/// <param name="sender">The MediatR sender.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("v1/Strava")]
public sealed partial class StravaWebhookController(
    IOptions<StravaOptions> options,
    ISender sender,
    ILogger<StravaWebhookController> logger) : ControllerBase
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    private readonly StravaOptions _options = options.Value;

    /// <summary>
    /// Validates a Strava webhook subscription by echoing the challenge token.
    /// </summary>
    /// <param name="request">The verification parameters from Strava.</param>
    /// <returns>
    /// A JSON object containing the <c>hub.challenge</c> value if the verify token matches;
    /// otherwise <c>403 Forbidden</c>.
    /// </returns>
    /// <response code="200">Returns the challenge value for verification.</response>
    /// <response code="403">If the verify token does not match.</response>
    [HttpGet("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult VerifyWebhook([FromQuery] StravaVerifyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Mode.Equals("subscribe", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(request.Challenge) ||
            request.VerifyToken != _options.WebhookVerifyToken)
        {
            LogWebhookVerifyFailed();
            return Forbid();
        }

        LogWebhookVerifySuccess();
        return Ok(new StravaWebhookVerificationResponse(request.Challenge));
    }

    /// <summary>
    /// Receives a real-time push event from Strava.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>200 OK</c> always (Strava requires fast acknowledgement).</returns>
    /// <response code="200">Event acknowledged.</response>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveEvent(CancellationToken cancellationToken)
    {
        try
        {
            var payload = await JsonSerializer.DeserializeAsync<StravaWebhookPayload>(
                Request.Body,
                cancellationToken: cancellationToken);
            if (payload is null ||
                (_options.WebhookSubscriptionId != 0 &&
                 payload.SubscriptionId != _options.WebhookSubscriptionId))
            {
                return Ok();
            }

            var externalUserId = payload.OwnerId.ToString(CultureInfo.InvariantCulture);
            if (payload.ObjectType.Equals("activity", StringComparison.OrdinalIgnoreCase) &&
                payload.AspectType.Equals("create", StringComparison.OrdinalIgnoreCase) &&
                payload.ObjectId > 0 &&
                payload.OwnerId > 0)
            {
                await _sender.Send(new EnqueueExternalActivitySyncCommand(
                    ExternalProvider.Strava.Id,
                    externalUserId,
                    ExternalActivitySyncTrigger.Webhook,
                    payload.GetIdempotencyKey(),
                    payload.ObjectId.ToString(CultureInfo.InvariantCulture)), cancellationToken);
            }
            else if (payload.OwnerId > 0 && payload.IsDeauthorization())
            {
                await _sender.Send(new EnqueueExternalConnectionRevocationCommand(
                    ExternalProvider.Strava.Id,
                    externalUserId,
                    payload.GetIdempotencyKey()), cancellationToken);
            }
            else
            {
                // Ignore other webhook events
            }
        }
        catch (JsonException exception)
        {
            LogWebhookIgnored(exception);
        }
        catch (InvalidOperationException exception)
        {
            // Strava requires acknowledgement even when internal scheduling is temporarily unavailable.
            LogWebhookSchedulingFailed(exception);
        }

        return Ok();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava webhook verification failed: invalid verify token")]
    private partial void LogWebhookVerifyFailed();

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava webhook subscription verified successfully")]
    private partial void LogWebhookVerifySuccess();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignoring an invalid Strava webhook payload")]
    private partial void LogWebhookIgnored(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unable to schedule Strava webhook work; event acknowledged")]
    private partial void LogWebhookSchedulingFailed(Exception exception);
}
