using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
using Adapters.Strava.Persistence;
using Adapters.Strava.Webhook;
using Domain.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
public sealed partial class StravaController(
    StravaApiClient stravaClient,
    IMemberRepository memberRepository,
    IClock clock,
    IOptions<StravaOptions> options,
    ILogger<StravaController> logger) : ControllerBase
{
    private readonly StravaOptions _options = options.Value;

    // ────────────────────────────────────────────────────────────────
    // OAuth Flow
    // ────────────────────────────────────────────────────────────────

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
        throw new NotImplementedException("Authorization endpoint will be implemented in Phase 1.");
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
        throw new NotImplementedException("Callback endpoint will be implemented in Phase 1.");
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
    // State Encryption (AES-GCM)
    // ────────────────────────────────────────────────────────────────

    private string EncryptState(Guid memberId)
    {
        var payload = JsonSerializer.Serialize(new OAuthStatePayload
        {
            MemberId = memberId,
            Nonce = Guid.NewGuid().ToString("N"),
            TimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        var key = Convert.FromBase64String(_options.StateEncryptionKey);
        var plaintext = Encoding.UTF8.GetBytes(payload);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Format: Base64(nonce + ciphertext + tag)
        var combined = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(combined);
    }

    private Guid? DecryptState(string encryptedState)
    {
        try
        {
            var combined = Convert.FromBase64String(encryptedState);
            var key = Convert.FromBase64String(_options.StateEncryptionKey);

            const int nonceSize = 12; // AesGcm.NonceByteSizes.MaxSize
            const int tagSize = 16;   // AesGcm.TagByteSizes.MaxSize

            if (combined.Length < nonceSize + tagSize)
            {
                return null;
            }

            var nonce = combined.AsSpan(0, nonceSize);
            var ciphertext = combined.AsSpan(nonceSize, combined.Length - nonceSize - tagSize);
            var tag = combined.AsSpan(combined.Length - tagSize, tagSize);
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, tagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            var payload = JsonSerializer.Deserialize<OAuthStatePayload>(Encoding.UTF8.GetString(plaintext));
            if (payload is null)
            {
                return null;
            }

            // Reject states older than 10 minutes
            var ageSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - payload.TimestampUtc;
            if (ageSeconds > 600) // 10 minutes
            {
                LogCallbackStateExpired(payload.MemberId);
                return null;
            }

            return payload.MemberId;
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // High-performance structured logging
    // ────────────────────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information, Message = "Redirecting member {MemberId} to Strava authorization")]
    private partial void LogAuthorizationRedirect(Guid memberId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback denied by user: {Error}")]
    private partial void LogCallbackDenied(string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback missing code or state parameters")]
    private partial void LogCallbackMissingParams();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback received invalid or tampered state")]
    private partial void LogCallbackInvalidState();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava OAuth callback state expired for member {MemberId}")]
    private partial void LogCallbackStateExpired(Guid memberId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Strava token exchange failed during callback for member {MemberId}")]
    private partial void LogCallbackTokenExchangeFailed(Guid memberId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Member {MemberId} not found during Strava callback")]
    private partial void LogCallbackMemberNotFound(Guid memberId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Strava connection established for member {MemberId}, athlete {AthleteId}")]
    private partial void LogCallbackSuccess(Guid memberId, long athleteId);

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
    /// Gets or sets the Planthor member identifier.
    /// </summary>
    public Guid MemberId { get; set; }

    /// <summary>
    /// Gets or sets a cryptographic nonce to prevent replay attacks.
    /// </summary>
    public string Nonce { get; set; } = default!;

    /// <summary>
    /// Gets or sets the UTC epoch seconds when the state was created.
    /// </summary>
    public long TimestampUtc { get; set; }
}
