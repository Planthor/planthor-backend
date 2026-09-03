using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava.Configuration;
using Adapters.Strava.Persistence;
using Api.Requests;
using Application.Dtos;
using Application.Members.Commands.ConnectExternalProvider;
using Application.Shared;
using Domain.Members;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace Api.Tests.Features.Strava;

/// <summary>
/// Exercises the production sync registrations end to end: post-commit domain events,
/// Quartz execution, Strava HTTP calls, adapter persistence, Mongo aggregates, and API reads.
/// </summary>
public sealed class StravaActivitySyncTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const long AthleteId = 987654321;
    private const long HistoricalActivityId = 700001;
    private const long WebhookActivityId = 700002;

    [Fact]
    public async Task ConnectionAndWebhook_CreateLinkedPlanLogsAndRemainIdempotent()
    {
        // Arrange: enable production auto-sync only for this scenario.
        using var syncFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                // The base test factory intentionally disables automatic imports. PostConfigure
                // guarantees this test override is applied after the production binding.
                services.PostConfigure<StravaOptions>(options => options.AutomaticSyncEnabled = true);

                // Identity synchronization is outside this scenario and would otherwise make a
                // network call for every authenticated request through the JIT session filter.
                services.RemoveAll<IKeycloakAdminClient>();
                services.AddSingleton<IKeycloakAdminClient, EmptyKeycloakAdminClient>();
            }));
        using var client = syncFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var testUserId = $"strava-sync-{Guid.NewGuid():N}";
        client.DefaultRequestHeaders.Add("X-TestUserId", testUserId);

        using (var optionsScope = syncFactory.Services.CreateScope())
        {
            Assert.True(optionsScope.ServiceProvider
                .GetRequiredService<IOptions<StravaOptions>>()
                .Value
                .AutomaticSyncEnabled);

        }

        var memberResponse = await client.PostAsJsonAsync(
            "/v1/members",
            new CreateMemberRequest("Strava", null, "Athlete", "Sync integration test", "UTC"));
        memberResponse.EnsureSuccessStatusCode();
        var memberDto = await memberResponse.Content.ReadFromJsonAsync<MemberDto>();
        Assert.NotNull(memberDto);

        var occurredAt = DateTimeOffset.UtcNow.AddDays(-1);
        var linkedPlan = await CreateAndActivatePlanAsync(client, "Linked running plan", true, occurredAt);
        var unlinkedPlan = await CreateAndActivatePlanAsync(client, "Manual-only running plan", false, occurredAt);

        string identifyName;
        var accessToken = $"sync-token-{Guid.NewGuid():N}";
        await using (var scope = syncFactory.Services.CreateAsyncScope())
        {
            var memberRepository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
            var member = await memberRepository.GetByIdAsync(memberDto.Id, CancellationToken.None);
            Assert.NotNull(member);
            identifyName = member.IdentifyName;

            var tokenDatabase = scope.ServiceProvider.GetRequiredService<StravaAdapterDatabase>();
            await tokenDatabase.UpsertAsync(new StravaTokenDocument
            {
                Id = identifyName,
                AthleteId = AthleteId,
                AccessToken = accessToken,
                RefreshToken = "integration-refresh-token",
                ExpiresAt = 2147483647,
                LastRefreshedAtUtc = DateTimeOffset.UtcNow
            }, CancellationToken.None);
        }

        factory.WireMockServer
            .Given(Request.Create()
                .WithPath("/api/v3/athlete/activities")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {accessToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        id = HistoricalActivityId,
                        name = "Historical run",
                        distance = 5000.0,
                        start_date = occurredAt,
                        type = "Run",
                        sport_type = "Run"
                    }
                })));

        // Act: the committed connection event must enqueue the historical import.
        await using (var scope = syncFactory.Services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new ConnectExternalProviderCommand(
                identifyName,
                ExternalProvider.Strava.Id,
                ExternalConnectionType.ActivitiesSync.Id,
                AthleteId.ToString(CultureInfo.InvariantCulture),
                ["activity:read_all"]));
        }

        await using (var scope = syncFactory.Services.CreateAsyncScope())
        {
            var tokenDatabase = scope.ServiceProvider.GetRequiredService<StravaAdapterDatabase>();
            var queuedToken = await tokenDatabase.GetByAthleteIdAsync(AthleteId, CancellationToken.None);
            Assert.NotNull(queuedToken);
            Assert.NotEqual("not_started", queuedToken.InitialSyncState);
        }

        var historicalLogs = await WaitForLogsAsync(client, linkedPlan.PlanId, expectedCount: 1);
        var historicalLog = Assert.Single(historicalLogs.Items);
        Assert.Equal(5f, historicalLog.Value);
        Assert.Equal("STRAVA", historicalLog.ExternalSourceProvider);
        Assert.Equal(HistoricalActivityId.ToString(CultureInfo.InvariantCulture), historicalLog.ExternalSourceId);
        Assert.Equal(occurredAt.ToUnixTimeSeconds(), historicalLog.CompletedDate.ToUnixTimeSeconds());

        var linkedAfterHistory = await ReadPlanAsync(client, linkedPlan.PlanId);
        Assert.Equal(5f, linkedAfterHistory.CurrentValue);
        var unlinkedLogs = await ReadLogsAsync(client, unlinkedPlan.PlanId);
        Assert.Empty(unlinkedLogs.Items);
        var initialStatus = await WaitForSyncStateAsync(client, "succeeded");
        Assert.Equal("succeeded", initialStatus.InitialSyncState);
        Assert.Equal("initial", initialStatus.LastTrigger);

        var webhookOccurredAt = occurredAt.AddHours(2);
        factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/api/v3/activities/{WebhookActivityId}")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {accessToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = WebhookActivityId,
                    name = "Webhook run",
                    distance = 2000.0,
                    start_date = webhookOccurredAt,
                    type = "Run",
                    sport_type = "Run",
                    athlete = new { id = AthleteId }
                })));

        var webhookPayload = new
        {
            object_type = "activity",
            object_id = WebhookActivityId,
            aspect_type = "create",
            owner_id = AthleteId,
            subscription_id = 99,
            event_time = webhookOccurredAt.ToUnixTimeSeconds(),
            updates = new { }
        };

        using var webhookClient = syncFactory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("X-Force-Unauthorized", "true");
        var webhookResponse = await webhookClient.PostAsJsonAsync("/v1/Strava/webhook", webhookPayload);
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var webhookLogs = await WaitForLogsAsync(client, linkedPlan.PlanId, expectedCount: 2);
        Assert.Contains(webhookLogs.Items, log =>
            log.ExternalSourceId == WebhookActivityId.ToString(CultureInfo.InvariantCulture) &&
            log.Value == 2f);
        var linkedAfterWebhook = await ReadPlanAsync(client, linkedPlan.PlanId);
        Assert.Equal(7f, linkedAfterWebhook.CurrentValue);
        var webhookStatus = await WaitForSyncStateAsync(client, "succeeded", "webhook");
        Assert.Equal("succeeded", webhookStatus.InitialSyncState);

        // Re-delivery can execute again after Quartz removes the first trigger; aggregate identity
        // still guarantees that no second ActivityLog is written.
        var duplicateResponse = await webhookClient.PostAsJsonAsync("/v1/Strava/webhook", webhookPayload);
        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        await WaitForProviderRequestCountAsync(
            factory,
            $"/api/v3/activities/{WebhookActivityId}",
            expectedCount: 2);
        Assert.NotNull(webhookStatus.LastSuccessfulSyncAt);
        await WaitForSyncStateAsync(
            client,
            "succeeded",
            "webhook",
            completedAfter: webhookStatus.LastSuccessfulSyncAt);

        var logsAfterReplay = await ReadLogsAsync(client, linkedPlan.PlanId);
        Assert.Equal(2, logsAfterReplay.Items.Count());
        var planAfterReplay = await ReadPlanAsync(client, linkedPlan.PlanId);
        Assert.Equal(7f, planAfterReplay.CurrentValue);
    }

    private static async Task<PersonalPlanDto> CreateAndActivatePlanAsync(
        HttpClient client,
        string name,
        bool linkUserAdapter,
        DateTimeOffset activityDate)
    {
        var from = activityDate.AddDays(-7);
        var to = activityDate.AddDays(7);
        var request = new CreatePersonalPlanRequest(
            name,
            "km",
            100,
            from,
            to,
            from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "UTC",
            EnableActivityLog: true,
            DisplayOnProfile: false,
            Prioritize: 1,
            LinkUserAdapter: linkUserAdapter,
            new CreateSportPlanDetailsRequest(["RUN"]));

        var createResponse = await client.PostAsJsonAsync("/v1/members/me/personal-plans", request);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<PersonalPlanDto>();
        Assert.NotNull(created);

        var activateResponse = await client.PostAsync(
            $"/v1/members/me/personal-plans/{created.PlanId}:activate",
            content: null);
        activateResponse.EnsureSuccessStatusCode();
        var activated = await activateResponse.Content.ReadFromJsonAsync<PersonalPlanDto>();
        Assert.NotNull(activated);
        return activated;
    }

    private static async Task<CursorPagedResult<ActivityLogDto>> WaitForLogsAsync(
        HttpClient client,
        Guid planId,
        int expectedCount)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var logs = await ReadLogsAsync(client, planId);
            if (logs.Items.Count() == expectedCount)
            {
                return logs;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Plan '{planId}' did not reach {expectedCount} activity logs.");
    }

    private static async Task<CursorPagedResult<ActivityLogDto>> ReadLogsAsync(HttpClient client, Guid planId)
    {
        var response = await client.GetAsync($"/v1/plans/{planId}/activity-logs?limit=100");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CursorPagedResult<ActivityLogDto>>()
            ?? throw new InvalidOperationException("The activity ledger response was empty.");
    }

    private static async Task<PersonalPlanDto> ReadPlanAsync(HttpClient client, Guid planId)
    {
        var response = await client.GetAsync($"/v1/members/me/personal-plans/{planId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PersonalPlanDto>()
            ?? throw new InvalidOperationException("The personal plan response was empty.");
    }

    private static async Task<ExternalActivitySyncStatusDto> WaitForSyncStateAsync(
        HttpClient client,
        string expectedState,
        string? expectedTrigger = null,
        DateTimeOffset? completedAfter = null)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var response = await client.GetAsync(
                "/v1/members/me/external-connections/STRAVA/sync-status");
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<ExternalActivitySyncStatusDto>();
                if (status?.State == expectedState &&
                    (expectedTrigger is null || status.LastTrigger == expectedTrigger) &&
                    (completedAfter is null || status.LastSuccessfulSyncAt > completedAfter))
                {
                    return status;
                }
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(
            $"Strava sync did not reach state '{expectedState}' for trigger '{expectedTrigger ?? "any"}'.");
    }

    private static async Task WaitForProviderRequestCountAsync(
        CustomWebApplicationFactory<Program> factory,
        string path,
        int expectedCount)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var count = factory.WireMockServer.LogEntries.Count(entry =>
                string.Equals(entry.RequestMessage?.Path, path, StringComparison.Ordinal));
            if (count >= expectedCount)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(
            $"Provider endpoint '{path}' did not receive {expectedCount} requests.");
    }

    private sealed class EmptyKeycloakAdminClient : IKeycloakAdminClient
    {
        public Task<List<FederatedIdentityDto>> GetUserFederatedIdentitiesAsync(string identifyName) =>
            Task.FromResult(new List<FederatedIdentityDto>());

        public Task<List<FederatedIdentityDto>> GetUserFederatedIdentitiesAsync(
            string identifyName,
            CancellationToken cancellationToken) =>
            Task.FromResult(new List<FederatedIdentityDto>());
    }
}
