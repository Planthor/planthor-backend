using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Requests;
using Application.Dtos;
using Application.Members.Commands.ConnectExternalProvider;
using Application.Shared;
using Domain.Members;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests.Features.Strava;

public class StravaControllerTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
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

    private sealed class RecordingBackgroundJobClient : IBackgroundJobClient
    {
        public ConcurrentQueue<ExternalActivitySyncJobRequest> ActivityRequests { get; } = new();
        public ConcurrentQueue<(string ProviderId, string ExternalUserId, string IdempotencyKey)> RevocationRequests { get; } = new();

        public Task EnqueueAvatarDownloadAsync(Guid memberId, Uri avatarUrl, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task EnqueueIdentitySyncAsync(Guid memberId, string identifyName, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task EnqueueExternalActivitySyncAsync(
            ExternalActivitySyncJobRequest request,
            CancellationToken cancellationToken)
        {
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
