using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
using Adapters.Strava.Webhook;
using Application.Members.Commands.ConnectExternalProvider;
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
[ApiController]
[Route("v1/[controller]")]
public sealed partial class StravaController : ControllerBase
{
    private readonly IStravaApiClient _stravaClient;
    private readonly IClock _clock;
    private readonly StravaOptions _options;
    private readonly ISender _sender;
    private readonly ILogger<StravaController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StravaController"/> class.
    /// </summary>
    /// <param name="stravaClient">The Strava API client.</param>
    /// <param name="clock">The system clock.</param>
    /// <param name="options">The Strava options.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="logger">The logger.</param>
    public StravaController(
        IStravaApiClient stravaClient,
        IClock clock,
        IOptions<StravaOptions> options,
        ISender sender,
        ILogger<StravaController> logger)
    {
        ArgumentNullException.ThrowIfNull(stravaClient);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(logger);

        _stravaClient = stravaClient;
        _clock = clock;
        _options = options.Value;
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Initiates the Strava OAuth authorization flow by redirecting the user
    /// to Strava's consent page.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A redirect to the Strava authorization URL.</returns>
    /// <response code="302">Redirects to Strava's OAuth consent page.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("authorize")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var identifyName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identifyName))
        {
            return Unauthorized();
        }

        var payload = new OAuthStatePayload
        {
            IdentifyName = identifyName,
            Nonce = Guid.NewGuid().ToString("N"),
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
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl, "error", error));
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            LogCallbackMissingParams();
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl, "error", "missing_params"));
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
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl, "error", "invalid_state"));
        }

        var nowEpoch = _clock.GetCurrentInstant().ToUnixTimeSeconds();
        if (nowEpoch - payload.TimestampUtc > 900) // 15 minutes expiration
        {
            LogCallbackStateExpired(payload.IdentifyName);
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl, "error", "state_expired"));
        }

        var tokenResponse = await _stravaClient.ExchangeCodeAsync(code, payload.IdentifyName, cancellationToken);
        if (tokenResponse == null)
        {
            LogCallbackTokenExchangeFailed(payload.IdentifyName);
            return Redirect(QueryHelpers.AddQueryString(_options.FrontendErrorUrl, "error", "exchange_failed"));
        }

        var scopesList = _options.Scopes.Split(',').Select(s => s.Trim()).ToList();

        await _sender.Send(new ConnectExternalProviderCommand(
            payload.IdentifyName,
            ExternalProvider.Strava.Id,
            ExternalConnectionType.ActivitiesSync.Id,
            tokenResponse.Athlete.Id.ToString(),
            scopesList
        ), cancellationToken);

        LogCallbackSuccess(payload.IdentifyName, tokenResponse.Athlete.Id);
        return Redirect(_options.FrontendSuccessUrl);
    }

    // ────────────────────────────────────────────────────────────────
    // Disconnect
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Disconnects the authenticated member's Strava account by revoking
    /// the OAuth tokens on Strava and updating the domain state.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><c>204 No Content</c> on success.</returns>
    /// <response code="204">The Strava connection was successfully revoked.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If no active Strava connection exists.</response>
    [HttpDelete("disconnect")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Disconnect endpoint will be implemented in Phase 1.");
    }

    // ────────────────────────────────────────────────────────────────
    // Webhook
    // ────────────────────────────────────────────────────────────────

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

        if (request.VerifyToken != _options.WebhookVerifyToken)
        {
            LogWebhookVerifyFailed();
            return Forbid();
        }

        LogWebhookVerifySuccess();
        return Ok(new { hub_challenge = request.Challenge });
    }

    /// <summary>
    /// Receives a real-time push event from Strava.
    /// </summary>
    /// <param name="payload">The Strava webhook event payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>200 OK</c> always (Strava requires fast acknowledgement).</returns>
    /// <response code="200">Event acknowledged.</response>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveEvent(
        [FromBody] StravaWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // Phase 3: Will enqueue a Quartz job for async processing
        await Task.CompletedTask;
        throw new NotSupportedException("Webhook event processing will be implemented in Phase 3.");
    }

    /// <summary>
    /// Triggers a manual incremental sync of Strava activities for the authenticated member.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>200 OK</c> with the number of new activity logs created.
    /// </returns>
    /// <response code="200">Sync completed successfully.</response>
    /// <response code="401">If the JWT is missing or invalid.</response>
    [HttpPost("sync")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ManualSync(CancellationToken cancellationToken)
    {
        // Phase 2: Will dispatch SyncStravaActivitiesCommand via MediatR
        await Task.CompletedTask;
        throw new NotSupportedException("Manual sync will be implemented in Phase 2.");
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
}

/// <summary>
/// Internal payload serialized into the encrypted OAuth <c>state</c> parameter.
/// </summary>
internal class OAuthStatePayload
{
    /// <summary>
    /// Gets or sets the Planthor member's Keycloak subject ID (Identity Name).
    /// </summary>
    public string IdentifyName { get; set; } = default!;

    /// <summary>
    /// Gets or sets a cryptographic nonce to prevent replay attacks.
    /// </summary>
    public string Nonce { get; set; } = default!;

    /// <summary>
    /// Gets or sets the UTC epoch seconds when the state was created.
    /// </summary>
    public long TimestampUtc { get; set; }
}
