using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava.Configuration;
using Api.Requests;
using Application.Dtos;
using Application.Members.Commands.ConnectExternalProvider;
using Application.Shared;
using Domain.Members;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace Api.Tests.Features.Strava;

public class StravaControllerTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string StateEncryptionKey = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";

    [Fact]
    public async Task Authorize_AuthenticatedMember_RedirectsWithEncryptedState()
    {
        // Arrange
        using var testFactory = CreateFactory(new RecordingBackgroundJobClient());
        using var client = CreateNonRedirectingClient(testFactory);
        client.DefaultRequestHeaders.Add("X-TestUserId", $"oauth-authorize-{Guid.NewGuid():N}");

        // Act
        var response = await client.GetAsync("/v1/Strava/authorize");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = Assert.IsType<Uri>(response.Headers.Location);
        Assert.Equal("www.strava.com", location.Host);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("test-client-id", query["client_id"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("force", query["approval_prompt"]);
        Assert.Equal("activity:read_all,profile:read_all", query["scope"]);

        var encryptedState = Assert.Single(query["state"]);
        Assert.NotNull(encryptedState);
        var stateJson = AesEncryptionHelper.Decrypt(encryptedState, StateEncryptionKey);
        var state = JsonSerializer.Deserialize<TestOAuthStatePayload>(stateJson);
        Assert.NotNull(state);
        Assert.NotEmpty(state.IdentifyName);
        Assert.NotEmpty(state.Nonce);
        Assert.True(state.TimestampUtc > 0);
    }

    [Fact]
    public async Task Callback_ValidAuthorization_ConnectsStravaAndRedirectsToSuccess()
    {
        // Arrange
        factory.WireMockServer.Reset();
        using var testFactory = CreateFactory(new RecordingBackgroundJobClient());
        using var client = CreateNonRedirectingClient(testFactory);
        client.DefaultRequestHeaders.Add("X-TestUserId", $"oauth-success-{Guid.NewGuid():N}");
        var authorizeResponse = await client.GetAsync("/v1/Strava/authorize");
        var state = ReadState(authorizeResponse);

        factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    access_token = "oauth-access-token",
                    refresh_token = "oauth-refresh-token",
                    expires_at = 2147483647L,
                    expires_in = 3600,
                    token_type = "Bearer",
                    athlete = new
                    {
                        id = 24680L,
                        firstname = "Test",
                        lastname = "Athlete"
                    }
                })));

        // Act
        var response = await client.GetAsync(
            $"/v1/Strava/callback?code=valid-code&state={Uri.EscapeDataString(state)}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "https://app.planthor.test/connections?status=success",
            response.Headers.Location?.ToString());

        var connectionsResponse = await client.GetAsync("/v1/members/me/external-connections");
        connectionsResponse.EnsureSuccessStatusCode();
        var connections = await connectionsResponse.Content.ReadFromJsonAsync<ExternalConnectionDto[]>();
        Assert.NotNull(connections);
        Assert.Contains(connections, connection =>
            connection.ProviderId == ExternalProvider.Strava.Id &&
            connection.ExternalUserId == "24680");
    }

    [Fact]
    public async Task Callback_WithProviderError_RedirectsToErrorDetails()
    {
        // Arrange
        using var client = CreateNonRedirectingClient(factory);

        // Act
        var response = await client.GetAsync("/v1/Strava/callback?error=access_denied");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "https://app.planthor.test/connections?status=error&error=access_denied",
            response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("?state=unused")]
    [InlineData("?code=unused")]
    public async Task Callback_WithMissingRequiredParameter_RedirectsToMissingParams(string query)
    {
        // Arrange
        using var client = CreateNonRedirectingClient(factory);

        // Act
        var response = await client.GetAsync($"/v1/Strava/callback{query}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("&error=missing_params", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(InvalidStateValues))]
    public async Task Callback_WithInvalidState_RedirectsToInvalidState(string state)
    {
        // Arrange
        using var client = CreateNonRedirectingClient(factory);

        // Act
        var response = await client.GetAsync(
            $"/v1/Strava/callback?code=unused&state={Uri.EscapeDataString(state)}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("&error=invalid_state", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Callback_WithExpiredState_RedirectsToStateExpired()
    {
        // Arrange
        using var client = CreateNonRedirectingClient(factory);
        var state = EncryptState(new TestOAuthStatePayload
        {
            IdentifyName = "EXPIRED_MEMBER",
            Nonce = Guid.NewGuid().ToString("N"),
            TimestampUtc = DateTimeOffset.UtcNow.AddMinutes(-16).ToUnixTimeSeconds()
        });

        // Act
        var response = await client.GetAsync(
            $"/v1/Strava/callback?code=unused&state={Uri.EscapeDataString(state)}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("&error=state_expired", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Callback_WhenTokenExchangeFails_RedirectsToExchangeFailed()
    {
        // Arrange
        factory.WireMockServer.Reset();
        using var client = CreateNonRedirectingClient(factory);
        var state = EncryptState(new TestOAuthStatePayload
        {
            IdentifyName = "EXCHANGE_FAILURE_MEMBER",
            Nonce = Guid.NewGuid().ToString("N"),
            TimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.BadRequest));

        // Act
        var response = await client.GetAsync(
            $"/v1/Strava/callback?code=rejected-code&state={Uri.EscapeDataString(state)}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("&error=exchange_failed", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    public static TheoryData<string> InvalidStateValues => new()
    {
        "not-base64",
        Convert.ToBase64String(new byte[17]),
        AesEncryptionHelper.Encrypt("not-json", StateEncryptionKey),
        AesEncryptionHelper.Encrypt("null", StateEncryptionKey),
        EncryptState(new TestOAuthStatePayload
        {
            IdentifyName = "",
            Nonce = "nonce",
            TimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        })
    };

    [Fact]
    public async Task VerifyWebhook_ValidModeAndToken_ReturnsExactChallengeProperty()
    {
        // Arrange
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");

        // Act
        var response = await client.GetAsync(
            "/v1/Strava/webhook?hub.verify_token=test-webhook-token&hub.challenge=test_challenge&hub.mode=subscribe");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("test_challenge", json.RootElement.GetProperty("hub.challenge").GetString());
    }

    [Theory]
    [InlineData("publish", "challenge", "test-webhook-token", HttpStatusCode.Forbidden)]
    [InlineData("subscribe", " ", "test-webhook-token", HttpStatusCode.BadRequest)]
    [InlineData("subscribe", "challenge", "wrong-token", HttpStatusCode.Forbidden)]
    public async Task VerifyWebhook_WithInvalidVerificationData_ReturnsExpectedRejection(
        string mode,
        string challenge,
        string token,
        HttpStatusCode expectedStatusCode)
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            $"/v1/Strava/webhook?hub.verify_token={Uri.EscapeDataString(token)}" +
            $"&hub.challenge={Uri.EscapeDataString(challenge)}" +
            $"&hub.mode={Uri.EscapeDataString(mode)}");

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveEvent_ActivityCreate_AcknowledgesAndEnqueuesWithoutProviderCall()
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");
        var payload = new
        {
            object_type = "activity",
            object_id = 456L,
            aspect_type = "create",
            owner_id = 123L,
            subscription_id = 99L,
            event_time = 1788307200L,
            updates = new { }
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/Strava/webhook", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(jobs.ActivityRequests);
        Assert.Equal("STRAVA", request.ProviderId);
        Assert.Equal("123", request.ExternalUserId);
        Assert.Equal("456", request.ExternalActivityId);
        Assert.Equal(ExternalActivitySyncTrigger.Webhook, request.Trigger);
    }

    [Fact]
    public async Task ReceiveEvent_UnsupportedOrMalformedPayload_AcknowledgesWithoutWork()
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");

        // Act
        var unsupported = await client.PostAsJsonAsync("/v1/Strava/webhook", new
        {
            object_type = "activity",
            object_id = 456L,
            aspect_type = "update",
            owner_id = 123L,
            subscription_id = 99L,
            event_time = 1788307200L,
            updates = new { }
        });
        using var invalidContent = new StringContent("{not-json", System.Text.Encoding.UTF8, "application/json");
        var malformed = await client.PostAsync("/v1/Strava/webhook", invalidContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, unsupported.StatusCode);
        Assert.Equal(HttpStatusCode.OK, malformed.StatusCode);
        Assert.Empty(jobs.ActivityRequests);
        Assert.Empty(jobs.RevocationRequests);
    }

    [Fact]
    public async Task ReceiveEvent_AthleteDeauthorization_AcknowledgesAndEnqueuesRevocation()
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");

        // Act
        var response = await client.PostAsJsonAsync("/v1/Strava/webhook", new
        {
            object_type = "athlete",
            object_id = 123L,
            aspect_type = "update",
            owner_id = 123L,
            subscription_id = 99L,
            event_time = 1788307200L,
            updates = new { authorized = "false" }
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var revocation = Assert.Single(jobs.RevocationRequests);
        Assert.Equal(("STRAVA", "123"), (revocation.ProviderId, revocation.ExternalUserId));
    }

    [Fact]
    public async Task ReceiveEvent_WithNullOrWrongSubscription_AcknowledgesWithoutWork()
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");
        using var nullContent = new StringContent("null", System.Text.Encoding.UTF8, "application/json");

        // Act
        var nullResponse = await client.PostAsync("/v1/Strava/webhook", nullContent);
        var wrongSubscriptionResponse = await client.PostAsJsonAsync("/v1/Strava/webhook", new
        {
            object_type = "activity",
            object_id = 456L,
            aspect_type = "create",
            owner_id = 123L,
            subscription_id = 100L,
            event_time = 1788307200L,
            updates = new { }
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, nullResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, wrongSubscriptionResponse.StatusCode);
        Assert.Empty(jobs.ActivityRequests);
        Assert.Empty(jobs.RevocationRequests);
    }

    [Theory]
    [InlineData(0L, 123L)]
    [InlineData(456L, 0L)]
    public async Task ReceiveEvent_WithInvalidActivityIdentifiers_AcknowledgesWithoutWork(
        long objectId,
        long ownerId)
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");

        // Act
        var response = await client.PostAsJsonAsync("/v1/Strava/webhook", new
        {
            object_type = "activity",
            object_id = objectId,
            aspect_type = "create",
            owner_id = ownerId,
            subscription_id = 99L,
            event_time = 1788307200L,
            updates = new { }
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(jobs.ActivityRequests);
        Assert.Empty(jobs.RevocationRequests);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("not-a-boolean")]
    public async Task ReceiveEvent_WithNonRevokingAthleteUpdate_AcknowledgesWithoutWork(string authorized)
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");

        // Act
        var response = await client.PostAsJsonAsync("/v1/Strava/webhook", new
        {
            object_type = "athlete",
            object_id = 123L,
            aspect_type = "update",
            owner_id = 123L,
            subscription_id = 99L,
            event_time = 1788307200L,
            updates = new { authorized }
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(jobs.ActivityRequests);
        Assert.Empty(jobs.RevocationRequests);
    }

    [Fact]
    public async Task ReceiveEvent_WithBooleanDeauthorization_AcknowledgesAndEnqueuesRevocation()
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");

        // Act
        var response = await client.PostAsJsonAsync("/v1/Strava/webhook", new
        {
            object_type = "athlete",
            object_id = 123L,
            aspect_type = "update",
            owner_id = 123L,
            subscription_id = 99L,
            event_time = 1788307200L,
            updates = new { authorized = false }
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(jobs.RevocationRequests);
    }

    [Fact]
    public async Task ReceiveEvent_WhenSchedulingFails_AcknowledgesWithoutPropagatingFailure()
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient
        {
            ThrowOnActivityEnqueue = true
        };
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");

        // Act
        var response = await client.PostAsJsonAsync("/v1/Strava/webhook", new
        {
            object_type = "activity",
            object_id = 456L,
            aspect_type = "create",
            owner_id = 123L,
            subscription_id = 99L,
            event_time = 1788307200L,
            updates = new { }
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(jobs.ActivityRequests);
    }

    [Fact]
    public async Task ManualSync_ActiveConnection_ReturnsAcceptedAndCoalescesJob()
    {
        // Arrange
        var jobs = new RecordingBackgroundJobClient();
        using var testFactory = CreateFactory(jobs);
        using var client = testFactory.CreateClient();
        var createMember = await client.PostAsJsonAsync(
            "/v1/members",
            new CreateMemberRequest("Sync", null, "Owner", "", "UTC"));
        createMember.EnsureSuccessStatusCode();
        var memberDto = await createMember.Content.ReadFromJsonAsync<MemberDto>();
        Assert.NotNull(memberDto);

        using (var scope = testFactory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var memberRepository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
            var member = await memberRepository.GetByIdAsync(memberDto.Id, CancellationToken.None);
            Assert.NotNull(member);
            await sender.Send(new ConnectExternalProviderCommand(
                member.IdentifyName,
                ExternalProvider.Strava.Id,
                ExternalConnectionType.ActivitiesSync.Id,
                "123",
                ["activity:read_all"]));
        }

        // Act
        var response = await client.PostAsync("/v1/Strava/sync", null);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"state\":\"queued\"", json, StringComparison.Ordinal);
        Assert.Single(jobs.ActivityRequests);
    }

    private WebApplicationFactory<Program> CreateFactory(RecordingBackgroundJobClient jobs) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBackgroundJobClient>();
            services.AddSingleton<IBackgroundJobClient>(jobs);
        }));

    private static HttpClient CreateNonRedirectingClient(WebApplicationFactory<Program> testFactory) =>
        testFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private static string ReadState(HttpResponseMessage authorizeResponse)
    {
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        var location = Assert.IsType<Uri>(authorizeResponse.Headers.Location);
        var state = Assert.Single(QueryHelpers.ParseQuery(location.Query)["state"]);
        Assert.NotNull(state);
        return state;
    }

    private static string EncryptState(TestOAuthStatePayload payload) =>
        AesEncryptionHelper.Encrypt(JsonSerializer.Serialize(payload), StateEncryptionKey);

    private sealed class TestOAuthStatePayload
    {
        public required string IdentifyName { get; init; }

        public required string Nonce { get; init; }

        public long TimestampUtc { get; init; }
    }

    private sealed class RecordingBackgroundJobClient : IBackgroundJobClient
    {
        public ConcurrentQueue<ExternalActivitySyncJobRequest> ActivityRequests { get; } = new();
        public ConcurrentQueue<(string ProviderId, string ExternalUserId, string IdempotencyKey)> RevocationRequests { get; } = new();
        public bool ThrowOnActivityEnqueue { get; init; }

        public Task EnqueueAvatarDownloadAsync(Guid memberId, Uri avatarUrl, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task EnqueueIdentitySyncAsync(Guid memberId, string identifyName, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task EnqueueExternalActivitySyncAsync(
            ExternalActivitySyncJobRequest request,
            CancellationToken cancellationToken)
        {
            if (ThrowOnActivityEnqueue)
            {
                return Task.FromException(new InvalidOperationException("Scheduling is unavailable."));
            }

            ActivityRequests.Enqueue(request);
            return Task.CompletedTask;
        }

        public Task EnqueueExternalConnectionRevocationAsync(
            string providerId,
            string externalUserId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            RevocationRequests.Enqueue((providerId, externalUserId, idempotencyKey));
            return Task.CompletedTask;
        }

        public Task CancelExternalActivitySyncAsync(
            string providerId,
            string externalUserId,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
