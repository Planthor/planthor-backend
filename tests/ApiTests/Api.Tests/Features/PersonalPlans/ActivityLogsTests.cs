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

        var createPlanResponse = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", createPlanCmd);
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
        var createLogResponse = await _client.PostAsJsonAsync($"/v1/plans/{planId}/activity-logs", createLogCmd);
        createLogResponse.EnsureSuccessStatusCode();
        
        var createdLog = await createLogResponse.Content.ReadFromJsonAsync<ActivityLogDto>();
        Assert.NotNull(createdLog);
        var logId = createdLog.Id;

        // 4. Read Activity Log
        var getResponse = await _client.GetAsync($"/v1/plans/{planId}/activity-logs/{logId}");
        getResponse.EnsureSuccessStatusCode();

        // 5. Read All Activity Logs
        var listResponse = await _client.GetAsync($"/v1/plans/{planId}/activity-logs");
        listResponse.EnsureSuccessStatusCode();

        // 6. Update Activity Log
        var updateResponse = await _client.PutAsync($"/v1/plans/{planId}/activity-logs/{logId}", null);
        updateResponse.EnsureSuccessStatusCode();

        // 7. Delete Activity Log
        var deleteResponse = await _client.DeleteAsync($"/v1/plans/{planId}/activity-logs/{logId}");
        deleteResponse.EnsureSuccessStatusCode();

        // 8. Patch Activity Logs (Not Supported)
        var patchResponse = await _client.PatchAsync($"/v1/plans/{planId}/activity-logs", null);
        Assert.Equal(HttpStatusCode.InternalServerError, patchResponse.StatusCode);
    }
    
    [Fact]
    public async Task ActivityLog_Security_Tests()
    {
        var planId = Guid.NewGuid();
        var logId = Guid.NewGuid();
        
        _client.DefaultRequestHeaders.Add("X-Omit-NameIdentifier", "true");
        
        var createCmd = new CreateActivityLogRequest(5.5f, "2026-07-10");
        var createRes = await _client.PostAsJsonAsync($"/v1/plans/{planId}/activity-logs", createCmd);
        Assert.Equal(HttpStatusCode.Unauthorized, createRes.StatusCode);

        var getList = await _client.GetAsync($"/v1/plans/{planId}/activity-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, getList.StatusCode);

        var getSingle = await _client.GetAsync($"/v1/plans/{planId}/activity-logs/{logId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getSingle.StatusCode);
        
        var updateRes = await _client.PutAsync($"/v1/plans/{planId}/activity-logs/{logId}", null);
        Assert.Equal(HttpStatusCode.Unauthorized, updateRes.StatusCode);

        var deleteRes = await _client.DeleteAsync($"/v1/plans/{planId}/activity-logs/{logId}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteRes.StatusCode);
        
        _client.DefaultRequestHeaders.Remove("X-Omit-NameIdentifier");
    }

    [Fact]
    public async Task ActivityLogs_List_Pagination_And_Validation_Tests()
    {
        // 1. Create Member & Plan
        await _client.PostAsJsonAsync("/v1/members", new CreateMemberRequest("Pagination", null, "User", "Test user", "UTC"));
        
        var createPlanCmd = new CreatePersonalPlanRequest(
            Name: "Pagination Plan", Unit: "km", Target: 100.0,
            FromDate: DateTimeOffset.UtcNow, ToDate: DateTimeOffset.UtcNow.AddDays(30),
            StartDateLocal: "2026-07-01", EndDateLocal: "2026-07-31",
            Timezone: "UTC", EnableActivityLog: true, DisplayOnProfile: true, Prioritize: 1, LinkUserAdapter: false,
            PlanDetails: new CreateSportPlanDetailsRequest(["Run"])
        );
        var createPlanResponse = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", createPlanCmd);
        var plan = await createPlanResponse.Content.ReadFromJsonAsync<PersonalPlanDto>();
        var planId = plan!.PlanId;

        // 2. Validate empty logs returns empty result (Branch: logs.Count == 0)
        var emptyListRes = await _client.GetAsync($"/v1/plans/{planId}/activity-logs");
        emptyListRes.EnsureSuccessStatusCode();
        var emptyList = await emptyListRes.Content.ReadFromJsonAsync<Application.Shared.CursorPagedResult<ActivityLogDto>>();
        Assert.Empty(emptyList!.Items);

        // Validate plan not found (Branch: plan == null)
        var notFoundPlanListRes = await _client.GetAsync($"/v1/plans/{Guid.NewGuid()}/activity-logs");
        Assert.Equal(HttpStatusCode.NotFound, notFoundPlanListRes.StatusCode);

        // 3. Validation limits (Branch: Limit validators)
        var invalidLimitRes = await _client.GetAsync($"/v1/plans/{planId}/activity-logs?limit=0");
        Assert.Equal(HttpStatusCode.BadRequest, invalidLimitRes.StatusCode);

        var overLimitRes = await _client.GetAsync($"/v1/plans/{planId}/activity-logs?limit=101");
        Assert.Equal(HttpStatusCode.BadRequest, overLimitRes.StatusCode);

        // 4. Create 3 logs with slight delays to ensure different CreatedAt
        for (int i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync($"/v1/plans/{planId}/activity-logs", new CreateActivityLogRequest(5f, "2026-07-10"));
            await Task.Delay(50); // slight delay to guarantee strictly descending CreatedAt
        }

        // 5. Paginate with Limit = 2
        var page1Res = await _client.GetAsync($"/v1/plans/{planId}/activity-logs?limit=2");
        page1Res.EnsureSuccessStatusCode();
        var page1 = await page1Res.Content.ReadFromJsonAsync<Application.Shared.CursorPagedResult<ActivityLogDto>>();
        
        var page1Items = new System.Collections.Generic.List<ActivityLogDto>(page1!.Items);
        Assert.Equal(2, page1Items.Count);
        Assert.True(page1.HasNextPage);
        Assert.NotNull(page1.NextCursor);

        // Check chronological descending order (Newest first)
        Assert.True(page1Items[0].CompletedDate >= page1Items[1].CompletedDate);

        // 6. Paginate next page using the cursor
        // We must UrlEncode the cursor since Base64 can contain '+' and '=' characters.
        var encodedCursor = Uri.EscapeDataString(page1.NextCursor);
        var page2Res = await _client.GetAsync($"/v1/plans/{planId}/activity-logs?limit=2&cursor={encodedCursor}");
        page2Res.EnsureSuccessStatusCode();
        var page2 = await page2Res.Content.ReadFromJsonAsync<Application.Shared.CursorPagedResult<ActivityLogDto>>();
        
        var page2Items = new System.Collections.Generic.List<ActivityLogDto>(page2!.Items);
        Assert.Single(page2Items);
        Assert.False(page2.HasNextPage);
        Assert.Null(page2.NextCursor);

        // 7. Invalid cursor format (Branch: invalid cursor parsing)
        var invalidCursorRes = await _client.GetAsync($"/v1/plans/{planId}/activity-logs?cursor=invalid_cursor_format");
        invalidCursorRes.EnsureSuccessStatusCode(); // Falls back to ignoring cursor
        var invalidCursorPage = await invalidCursorRes.Content.ReadFromJsonAsync<Application.Shared.CursorPagedResult<ActivityLogDto>>();
        var invalidCursorItems = new System.Collections.Generic.List<ActivityLogDto>(invalidCursorPage!.Items);
        Assert.Equal(3, invalidCursorItems.Count); // Returned all 3 since default limit is 10
    }
}
