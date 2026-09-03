using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
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
/// Handles Strava OAuth authorization flow.
/// </summary>
/// <param name="stravaClient">The Strava API client.</param>
/// <param name="clock">The system clock.</param>
/// <param name="options">The Strava options.</param>
/// <param name="sender">The MediatR sender.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("v1/Strava")]
public sealed partial class StravaOAuthController(
    IStravaApiClient stravaClient,
    IClock clock,
    IOptions<StravaOptions> options,
    ISender sender,
    ILogger<StravaOAuthController> logger) : ControllerBase
{
    private const long StateExpirationInSeconds = 900;
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

        var redirectUri = Url.Action(nameof(Callback), "StravaOAuth", null, Request.Scheme);
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
        if (nowEpoch - payload.TimestampUtc > StateExpirationInSeconds) // 15 minutes expiration
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
}
