using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
using Adapters.Strava.Webhook;
using Application.ExternalSync.Commands.EnqueueExternalActivitySync;
using Application.ExternalSync.Commands.EnqueueExternalConnectionRevocation;
using Application.ExternalSync.Commands.RequestExternalActivitySync;
using Application.Members.Commands.ConnectExternalProvider;
using Application.Shared;
using Domain.Members;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Adapters.Strava.Controllers;

/// <summary>
/// Handles Strava-specific endpoints: OAuth authorization, webhook subscription verification,
/// real-time push event processing, manual activity sync, and disconnection.
/// </summary>
/// <param name="stravaClient">The Strava API client.</param>
/// <param name="clock">The system clock.</param>
/// <param name="options">The Strava options.</param>
/// <param name="sender">The MediatR sender.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("v1/[controller]")]
public sealed partial class StravaController(
    IStravaApiClient stravaClient,
    IClock clock,
    IOptions<StravaOptions> options,
    ISender sender,
    ILogger<StravaController> logger) : ControllerBase
{
    private readonly IStravaApiClient _stravaClient = stravaClient ?? throw new ArgumentNullException(nameof(stravaClient));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    private readonly StravaOptions _options = options.Value;

    /// <summary>
    /// Initiates the Strava OAuth authorization flow by redirecting the user
    /// to Strava's consent page.
    /// </summary>
    /// <returns>A redirect to the Strava authorization URL.</returns>
    /// <response code="302">Redirects to Strava's OAuth consent page.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("authorize")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Authorize()
    {
        var identifyName = HttpContext.Items["IdentifyName"] as string;
        if (string.IsNullOrEmpty(identifyName))
        {
            return Unauthorized();
        }

        var payload = new OAuthStatePayload
        {
            IdentifyName = identifyName,
            Nonce = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            TimestampUtc = _clock.GetCurrentInstant().ToUnixTimeSeconds()
        };

        var json = JsonSerializer.Serialize(payload);
        var encryptedState = AesEncryptionHelper.Encrypt(json, _options.StateEncryptionKey);

        var redirectUri = Url.Action(nameof(Callback), "Strava", null, Request.Scheme);
        if (string.IsNullOrEmpty(redirectUri))
        {
            return BadRequest("Could not generate redirect URI.");
        }

        var authorizeUrl = $"https://www.strava.com/oauth/authorize" +
                           $"?client_id={_options.ClientId}" +
                           $"&response_type=code" +
                           $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                           $"&approval_prompt=force" +
                           $"&scope={Uri.EscapeDataString(_options.Scopes)}" +
                           $"&state={Uri.EscapeDataString(encryptedState)}";

        LogAuthorizationRedirect(identifyName);

        return Redirect(authorizeUrl);
    }

    /// <summary>
    /// Handles the OAuth callback from Strava after the user grants or denies authorization.
    /// Exchanges the authorization code for tokens and updates the member's external connection.
    /// </summary>
    /// <param name="code">The authorization code from Strava.</param>
    /// <param name="state">The encrypted state parameter for CSRF protection.</param>
    /// <param name="error">An optional error string if the user denied access.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A redirect to the frontend success or error URL.</returns>
    /// <response code="302">Redirects to the frontend after processing.</response>
    [HttpGet("callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error))
        {
            LogCallbackDenied(error);
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl.ToString(), "error", error));
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            LogCallbackMissingParams();
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl.ToString(), "error", "missing_params"));
        }

        OAuthStatePayload? payload = null;
        try
        {
            var decryptedJson = AesEncryptionHelper.Decrypt(state, _options.StateEncryptionKey);
            payload = JsonSerializer.Deserialize<OAuthStatePayload>(decryptedJson);
        }
        catch (Exception ex) when (ex is CryptographicException ||
                                   ex is JsonException ||
                                   ex is FormatException)
        {
            LogCallbackInvalidState(ex);
        }

        if (payload == null || string.IsNullOrEmpty(payload.IdentifyName))
        {
            if (payload == null)
            {
                LogCallbackInvalidState(new InvalidOperationException("Decrypted state payload was null or empty."));
            }
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl.ToString(), "error", "invalid_state"));
        }

        var nowEpoch = _clock.GetCurrentInstant().ToUnixTimeSeconds();
        if (nowEpoch - payload.TimestampUtc > 900) // 15 minutes expiration
        {
            LogCallbackStateExpired(payload.IdentifyName);
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl.ToString(), "error", "state_expired"));
        }

        var tokenResponse = await _stravaClient.ExchangeCodeAsync(code, payload.IdentifyName, cancellationToken);
        if (tokenResponse == null)
        {
            LogCallbackTokenExchangeFailed(payload.IdentifyName);
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl.ToString(), "error", "exchange_failed"));
        }

        var scopesList = _options.Scopes.Split(',').Select(s => s.Trim()).ToList();

        await _sender.Send(new ConnectExternalProviderCommand(
            payload.IdentifyName,
            ExternalProvider.Strava.Id,
            ExternalConnectionType.ActivitiesSync.Id,
            tokenResponse.Athlete.Id.ToString(CultureInfo.InvariantCulture),
            scopesList
        ), cancellationToken);

        LogCallbackSuccess(payload.IdentifyName, tokenResponse.Athlete.Id);
        return Redirect(_options.FrontendSuccessUrl.ToString());
    }

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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Strava requires acknowledgement even when internal scheduling is temporarily unavailable.
            LogWebhookSchedulingFailed(exception);
        }

        return Ok();
    }

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

    // ────────────────────────────────────────────────────────────────
    // High-performance structured logging
    // ────────────────────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information, Message = "Redirecting member {IdentifyName} to Strava authorization")]
    private partial void LogAuthorizationRedirect(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback denied by user: {Error}")]
    private partial void LogCallbackDenied(string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback missing code or state parameters")]
    private partial void LogCallbackMissingParams();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback received invalid or tampered state")]
    private partial void LogCallbackInvalidState(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback state expired for member {IdentifyName}")]
    private partial void LogCallbackStateExpired(string identifyName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Strava token exchange failed during callback for member {IdentifyName}")]
    private partial void LogCallbackTokenExchangeFailed(string identifyName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava connection established for member {IdentifyName}, athlete {AthleteId}")]
    private partial void LogCallbackSuccess(string identifyName, long athleteId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava connection disconnected for member {MemberId}")]
    private partial void LogDisconnectSuccess(Guid memberId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava webhook verification failed: invalid verify token")]
    private partial void LogWebhookVerifyFailed();

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava webhook subscription verified successfully")]
    private partial void LogWebhookVerifySuccess();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignoring an invalid Strava webhook payload")]
    private partial void LogWebhookIgnored(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unable to schedule Strava webhook work; event acknowledged")]
    private partial void LogWebhookSchedulingFailed(Exception exception);
}
