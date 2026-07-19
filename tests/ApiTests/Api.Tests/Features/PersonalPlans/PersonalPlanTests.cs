using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Api.Requests;
using Application.Dtos;
using Xunit;

namespace Api.Tests.Features.PersonalPlans;

public class PersonalPlanTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PersonalPlanTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PersonalPlan_Lifecycle_Tests()
    {
        // We first need a member for the identity
        var createMemberCmd = new Api.Requests.CreateMemberRequest(
            FirstName: "Plan",
            MiddleName: null,
            LastName: "Owner",
            Description: "For testing plans",
            PreferredTimezone: "UTC"
        );
        var createMemberResponse = await _client.PostAsJsonAsync("/v1/members", createMemberCmd);
        createMemberResponse.EnsureSuccessStatusCode();

        // 1. Create Personal Plan
        var createCmd = new CreatePersonalPlanRequest(
            Name: "Test Plan",
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
            LinkUserAdapter: false
        );

        var createResponse = await _client.PostAsJsonAsync("/v1/members/me/personalPlans", createCmd);
        createResponse.EnsureSuccessStatusCode();
        
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PersonalPlanDto>();
        Assert.NotNull(createdPlan);
        Assert.Equal(100.0, createdPlan.Target);

        var planId = createdPlan.PlanId;
        
        // 2. Read Plan
        var getResponse = await _client.GetAsync($"/v1/members/me/personalPlans/{planId}");
        getResponse.EnsureSuccessStatusCode();

        // 3. Update Plan
        var updateCmd = new UpdatePersonalPlanRequest(
            Unit: "km",
            Target: 100.0,
            Current: 10.0,
            FromDate: DateTimeOffset.UtcNow,
            ToDate: DateTimeOffset.UtcNow.AddDays(30),
            PeriodType: "Custom"
        );
        var updateResponse = await _client.PutAsJsonAsync($"/v1/members/me/personalPlans/{planId}", updateCmd);
        if (!updateResponse.IsSuccessStatusCode)
        {
            var err = await updateResponse.Content.ReadAsStringAsync();
            throw new Exception($"Update failed with {updateResponse.StatusCode}: {err}");
        }
        // 4. Read All Plans
        var listResponse = await _client.GetAsync("/v1/members/me/personalPlans");
        listResponse.EnsureSuccessStatusCode();

        // 5. Cancel Plan
        var cancelResponse = await _client.PostAsync($"/v1/members/me/personalPlans/{planId}:cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        // 6. Patch Plan (Not Supported)
        var patchResponse = await _client.PatchAsync("/v1/members/me/personalPlans", null);
        Assert.Equal(HttpStatusCode.InternalServerError, patchResponse.StatusCode);

        // 6. Activate Plan
        var activateResponse = await _client.PostAsync($"/v1/members/me/personalPlans/{planId}:activate", null);
        activateResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PersonalPlan_Security_Tests()
    {
        // 1. Unauthorized due to missing NameIdentifier (controller logic)
        _client.DefaultRequestHeaders.Add("X-Omit-NameIdentifier", "true");
        
        var getList = await _client.GetAsync("/v1/members/me/personalPlans");
        Assert.Equal(HttpStatusCode.Unauthorized, getList.StatusCode);

        var getSingle = await _client.GetAsync("/v1/members/me/personalPlans/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.Unauthorized, getSingle.StatusCode);

        var createCmd = new CreatePersonalPlanRequest("Test", "km", 10.0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "2026-07-01", "2026-07-31", "UTC", true, true, 1, false);
        var createRes = await _client.PostAsJsonAsync("/v1/members/me/personalPlans", createCmd);
        Assert.Equal(HttpStatusCode.Unauthorized, createRes.StatusCode);

        var updateCmd = new UpdatePersonalPlanRequest("km", 10.0, 5.0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), "Custom");
        var updateRes = await _client.PutAsJsonAsync("/v1/members/me/personalPlans/00000000-0000-0000-0000-000000000000", updateCmd);
        Assert.Equal(HttpStatusCode.Unauthorized, updateRes.StatusCode);

        var cancelRes = await _client.PostAsync("/v1/members/me/personalPlans/00000000-0000-0000-0000-000000000000:cancel", null);
        Assert.Equal(HttpStatusCode.Unauthorized, cancelRes.StatusCode);

        var activateRes = await _client.PostAsync("/v1/members/me/personalPlans/00000000-0000-0000-0000-000000000000:activate", null);
        Assert.Equal(HttpStatusCode.Unauthorized, activateRes.StatusCode);

        _client.DefaultRequestHeaders.Remove("X-Omit-NameIdentifier");

        // 2. Forbid (using another-user identifier)
        var createForbid = await _client.PostAsJsonAsync("/v1/members/another-user/personalPlans", createCmd);
        Assert.Equal(HttpStatusCode.Forbidden, createForbid.StatusCode);

        var updateForbid = await _client.PutAsJsonAsync("/v1/members/another-user/personalPlans/00000000-0000-0000-0000-000000000000", updateCmd);
        Assert.Equal(HttpStatusCode.Forbidden, updateForbid.StatusCode);

        var cancelForbid = await _client.PostAsync("/v1/members/another-user/personalPlans/00000000-0000-0000-0000-000000000000:cancel", null);
        Assert.Equal(HttpStatusCode.Forbidden, cancelForbid.StatusCode);

        var activateForbid = await _client.PostAsync("/v1/members/another-user/personalPlans/00000000-0000-0000-0000-000000000000:activate", null);
        Assert.Equal(HttpStatusCode.Forbidden, activateForbid.StatusCode);

        // 3. BadRequest (Null command) handled by ASP.NET Core MVC
        var nullUpdate = await _client.PutAsJsonAsync<UpdatePersonalPlanRequest>("/v1/members/me/personalPlans/00000000-0000-0000-0000-000000000000", null);
        Assert.Equal(HttpStatusCode.BadRequest, nullUpdate.StatusCode);    
        // 4. Create BadRequest (Null command)
        var res4 = await _client.PostAsJsonAsync<CreatePersonalPlanRequest>("/v1/members/me/personalPlans", null);
        Assert.Equal(HttpStatusCode.BadRequest, res4.StatusCode);
    }
}
