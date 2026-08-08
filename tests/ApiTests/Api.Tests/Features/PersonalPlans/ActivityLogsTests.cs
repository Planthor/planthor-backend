using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Api.Requests;
using Application.Dtos;
using Xunit;

namespace Api.Tests.Features.PersonalPlans;

public class ActivityLogsTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ActivityLog_Lifecycle_Tests()
    {
        // 1. Create Member
        var createMemberCmd = new CreateMemberRequest("Activity", null, "Logger", "Test user", "UTC");
        var createMemberResponse = await _client.PostAsJsonAsync("/v1/members", createMemberCmd);
        createMemberResponse.EnsureSuccessStatusCode();

        // 2. Create Personal Plan
        var createPlanCmd = new CreatePersonalPlanRequest(
            Name: "Test Plan for Logs",
            Unit: "km",
            Target: 100.0,
            FromDate: DateTimeOffset.UtcNow,
            ToDate: DateTimeOffset.UtcNow.AddDays(30),
            StartDateLocal: "2026-07-01",
            EndDateLocal: "2026-07-31",
            Timezone: "UTC",
            EnableActivityLog: true,
            DisplayOnProfile: true,
            Prioritize: 1,
            LinkUserAdapter: false,
            PlanDetails: new CreateSportPlanDetailsRequest(["Run", "Ride"])
        );

        var createPlanResponse = await _client.PostAsJsonAsync("/v1/members/me/personalPlans", createPlanCmd);
        createPlanResponse.EnsureSuccessStatusCode();
        var createdPlan = await createPlanResponse.Content.ReadFromJsonAsync<PersonalPlanDto>();
        var planId = createdPlan!.PlanId;

        // 3. Create Activity Log
        var createLogCmd = new CreateActivityLogRequest(
            Value: 5.5f,
            ActivityLocalDate: "2026-07-10",
            ExternalProviderId: "STRAVA",
            ExternalActivityId: "12345"
        );
        var createLogResponse = await _client.PostAsJsonAsync($"/v1/plans/{planId}/ActivityLogs", createLogCmd);
        createLogResponse.EnsureSuccessStatusCode();
        
        var createdLog = await createLogResponse.Content.ReadFromJsonAsync<ActivityLogDto>();
        Assert.NotNull(createdLog);
        var logId = createdLog.Id;

        // 4. Read Activity Log
        var getResponse = await _client.GetAsync($"/v1/plans/{planId}/ActivityLogs/{logId}");
        getResponse.EnsureSuccessStatusCode();

        // 5. Read All Activity Logs
        var listResponse = await _client.GetAsync($"/v1/plans/{planId}/ActivityLogs");
        listResponse.EnsureSuccessStatusCode();

        // 6. Update Activity Log
        var updateResponse = await _client.PutAsync($"/v1/plans/{planId}/ActivityLogs/{logId}", null);
        updateResponse.EnsureSuccessStatusCode();

        // 7. Delete Activity Log
        var deleteResponse = await _client.DeleteAsync($"/v1/plans/{planId}/ActivityLogs/{logId}");
        deleteResponse.EnsureSuccessStatusCode();

        // 8. Patch Activity Logs (Not Supported)
        var patchResponse = await _client.PatchAsync($"/v1/plans/{planId}/ActivityLogs", null);
        Assert.Equal(HttpStatusCode.InternalServerError, patchResponse.StatusCode);
    }
    
    [Fact]
    public async Task ActivityLog_Security_Tests()
    {
        var planId = Guid.NewGuid();
        var logId = Guid.NewGuid();
        
        _client.DefaultRequestHeaders.Add("X-Omit-NameIdentifier", "true");
        
        var createCmd = new CreateActivityLogRequest(5.5f, "2026-07-10");
        var createRes = await _client.PostAsJsonAsync($"/v1/plans/{planId}/ActivityLogs", createCmd);
        Assert.Equal(HttpStatusCode.Unauthorized, createRes.StatusCode);

        var getList = await _client.GetAsync($"/v1/plans/{planId}/ActivityLogs");
        Assert.Equal(HttpStatusCode.Unauthorized, getList.StatusCode);

        var getSingle = await _client.GetAsync($"/v1/plans/{planId}/ActivityLogs/{logId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getSingle.StatusCode);
        
        var updateRes = await _client.PutAsync($"/v1/plans/{planId}/ActivityLogs/{logId}", null);
        Assert.Equal(HttpStatusCode.Unauthorized, updateRes.StatusCode);

        var deleteRes = await _client.DeleteAsync($"/v1/plans/{planId}/ActivityLogs/{logId}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteRes.StatusCode);
        
        _client.DefaultRequestHeaders.Remove("X-Omit-NameIdentifier");
    }
}
