using Microsoft.AspNetCore.Mvc;

namespace Backend.Adapters.Strava.Webhook;

/// <summary>
/// Represents the query-string parameters Strava sends when verifying a new webhook subscription.
/// </summary>
/// <remarks>
/// When you register a webhook subscription with Strava, Strava sends a GET request to your
/// callback URL with these query parameters. You must respond with the <c>hub.challenge</c>
/// value to confirm ownership of the URL.
/// See: https://developers.strava.com/docs/webhooks/#the-webhook-flow
/// </remarks>
public class StravaVerifyRequest
{
    /// <summary>
    /// Gets or sets the hub mode. Expected value is <c>"subscribe"</c>.
    /// </summary>
    [FromQuery(Name = "hub.mode")]
    public string Mode { get; set; } = default!;

    /// <summary>
    /// Gets or sets the random challenge string Strava expects echoed back.
    /// </summary>
    [FromQuery(Name = "hub.challenge")]
    public string Challenge { get; set; } = default!;

    /// <summary>
    /// Gets or sets the verify token we supplied when registering the subscription.
    /// Must match <c>StravaOptions.WebhookVerifyToken</c>.
    /// </summary>
    [FromQuery(Name = "hub.verify_token")]
    public string VerifyToken { get; set; } = default!;
}
