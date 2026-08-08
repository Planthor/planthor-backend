using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
using Adapters.Strava.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Testing;
using NSubstitute;

namespace Adapters.Tests.Strava.Controllers;

/// <summary>
/// Contains unit tests for the <see cref="StravaController"/> to ensure high code coverage
/// for the Strava OAuth flow (Authorize and Callback).
/// </summary>
public class StravaControllerTests
{
    private readonly IStravaApiClient _stravaClient;
    private readonly IClock _clock;
    private readonly IOptions<StravaOptions> _options;
    private readonly ISender _sender;
    private readonly ILogger<StravaController> _logger;
    private readonly StravaController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="StravaControllerTests"/> class,
    /// mocking all necessary dependencies via NSubstitute.
    /// </summary>
    public StravaControllerTests()
    {
        _stravaClient = Substitute.For<IStravaApiClient>();
        _clock = new FakeClock(Instant.FromUtc(2025, 1, 1, 12, 0, 0));
        
        var options = new StravaOptions
        {
            ClientId = "test-client",
            StateEncryptionKey = "12345678901234567890123456789012", // 32 chars
            FrontendErrorUrl = new Uri("https://frontend.com/error"),
            FrontendSuccessUrl = new Uri("https://frontend.com/success"),
            Scopes = "read,activity:read"
        };
        _options = Options.Create(options);
        
        _sender = Substitute.For<ISender>();
        _logger = Substitute.For<ILogger<StravaController>>();

        _controller = new StravaController(
            _stravaClient,
            _clock,
            _options,
            _sender,
            _logger
        );

        // Setup User identity
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "test-user-id") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = user };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.Action(Arg.Any<UrlActionContext>()).Returns("https://localhost/v1/Strava/callback");

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.Url = urlHelper;
    }

    /// <summary>
    /// Verifies that calling the Authorize endpoint returns a proper redirect to Strava
    /// with the correct client ID and an encrypted state payload.
    /// </summary>
    [Fact]
    public async Task Authorize_ShouldRedirectToStravaWithEncryptedState()
    {
        // Act
        var result = await _controller.Authorize(CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("https://www.strava.com/oauth/authorize", redirectResult.Url);
        Assert.Contains("client_id=test-client", redirectResult.Url);
        Assert.Contains("state=", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that calling Authorize without an identity returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task Authorize_WithoutIdentity_ShouldReturnUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await _controller.Authorize(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Verifies that Authorize returns BadRequest if UrlHelper cannot generate a redirect URL.
    /// </summary>
    [Fact]
    public async Task Authorize_WithNullRedirectUri_ShouldReturnBadRequest()
    {
        // Arrange
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.Action(Arg.Any<UrlActionContext>()).Returns((string?)null);
        _controller.Url = urlHelper;

        // Act
        var result = await _controller.Authorize(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Could not generate redirect URI.", badRequestResult.Value);
    }

    /// <summary>
    /// Verifies that if Strava returns an error in the query parameters,
    /// the callback redirects to the frontend error URL.
    /// </summary>
    [Fact]
    public async Task Callback_WithError_ShouldRedirectToFrontendError()
    {
        // Act
        var result = await _controller.Callback(null, null, "access_denied", CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://frontend.com/error?error=access_denied", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that if either the code or state is missing,
    /// the callback redirects to the frontend error URL.
    /// </summary>
    /// <param name="code">The authorization code.</param>
    /// <param name="state">The encrypted state.</param>
    [Theory]
    [InlineData(null, "state")]
    [InlineData("code", null)]
    public async Task Callback_WithMissingParams_ShouldRedirectToFrontendError(string? code, string? state)
    {
        // Act
        var result = await _controller.Callback(code, state, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://frontend.com/error?error=missing_params", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that if the state parameter is tampered with or fails decryption,
    /// the callback redirects to the frontend error URL.
    /// </summary>
    [Fact]
    public async Task Callback_WithInvalidState_ShouldRedirectToFrontendError()
    {
        // Act
        var result = await _controller.Callback("valid_code", "invalid_encrypted_state_string", null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://frontend.com/error?error=invalid_state", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that if the state payload timestamp is older than 15 minutes,
    /// the callback redirects to the frontend error URL.
    /// </summary>
    [Fact]
    public async Task Callback_WithExpiredState_ShouldRedirectToFrontendError()
    {
        // Arrange
        var expiredEpoch = _clock.GetCurrentInstant().ToUnixTimeSeconds() - 1000; // 1000 seconds ago (> 15 mins)
        var payload = new OAuthStatePayload { TimestampUtc = expiredEpoch, IdentifyName = "test-user", Nonce = "nonce" };
        var json = JsonSerializer.Serialize(payload);
        var encryptedState = AesEncryptionHelper.Encrypt(json, _options.Value.StateEncryptionKey);

        // Act
        var result = await _controller.Callback("valid_code", encryptedState, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://frontend.com/error?error=state_expired", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that if the Strava API client fails to exchange the authorization code,
    /// the callback redirects to the frontend error URL.
    /// </summary>
    [Fact]
    public async Task Callback_ExchangeTokenFailed_ShouldRedirectToFrontendError()
    {
        // Arrange
        var epoch = _clock.GetCurrentInstant().ToUnixTimeSeconds();
        var payload = new OAuthStatePayload { TimestampUtc = epoch, IdentifyName = "test-user", Nonce = "nonce" };
        var json = JsonSerializer.Serialize(payload);
        var encryptedState = AesEncryptionHelper.Encrypt(json, _options.Value.StateEncryptionKey);

        _stravaClient.ExchangeCodeAsync("valid_code", "test-user", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<StravaTokenResponse?>(null));

        // Act
        var result = await _controller.Callback("valid_code", encryptedState, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://frontend.com/error?error=exchange_failed", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that upon a successful token exchange, the callback redirects
    /// to the frontend success URL.
    /// </summary>
    [Fact]
    public async Task Callback_Success_ShouldRedirectToFrontendSuccess()
    {
        // Arrange
        var epoch = _clock.GetCurrentInstant().ToUnixTimeSeconds();
        var payload = new OAuthStatePayload { TimestampUtc = epoch, IdentifyName = "test-user", Nonce = "nonce" };
        var json = JsonSerializer.Serialize(payload);
        var encryptedState = AesEncryptionHelper.Encrypt(json, _options.Value.StateEncryptionKey);

        var tokenResponse = new StravaTokenResponse
        {
            AccessToken = "acc",
            RefreshToken = "ref",
            ExpiresAt = 1234,
            Athlete = new StravaAthleteInfo { Id = 777 }
        };

        _stravaClient.ExchangeCodeAsync("valid_code", "test-user", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<StravaTokenResponse?>(tokenResponse));

        // Act
        var result = await _controller.Callback("valid_code", encryptedState, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://frontend.com/success", redirectResult.Url);
        await _sender.Received(1).Send(Arg.Is<Application.Members.Commands.ConnectExternalProvider.ConnectExternalProviderCommand>(
            c => c != null && c.IdentifyName == "test-user" && c.ExternalUserId == "777" && c.Scopes != null && c.Scopes.Count == 2
        ), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that Webhook Verify returns the challenge if the token matches.
    /// </summary>
    [Fact]
    public void VerifyWebhook_WithValidToken_ShouldReturnChallenge()
    {
        // Arrange
        _options.Value.WebhookVerifyToken = "valid_token";
        var request = new Adapters.Strava.Webhook.StravaVerifyRequest { VerifyToken = "valid_token", Challenge = "test_challenge" };

        // Act
        var result = _controller.VerifyWebhook(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Verifies that Webhook Verify returns Forbidden if the token does not match.
    /// </summary>
    [Fact]
    public void VerifyWebhook_WithInvalidToken_ShouldReturnForbidden()
    {
        // Arrange
        _options.Value.WebhookVerifyToken = "valid_token";
        var request = new Adapters.Strava.Webhook.StravaVerifyRequest { VerifyToken = "invalid_token", Challenge = "test_challenge" };

        // Act
        var result = _controller.VerifyWebhook(request);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Verifies that NotImplemented endpoints throw appropriate exceptions.
    /// </summary>
    [Fact]
    public async Task NotImplementedEndpoints_ShouldThrowExceptions()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() => _controller.Disconnect(CancellationToken.None));
        await Assert.ThrowsAsync<NotSupportedException>(() => _controller.ReceiveEvent(new Adapters.Strava.Webhook.StravaWebhookPayload(), CancellationToken.None));
        await Assert.ThrowsAsync<NotSupportedException>(() => _controller.ManualSync(CancellationToken.None));
    }
}
