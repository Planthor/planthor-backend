using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Api.Requests;
using Application.Dtos;
using Xunit;

namespace Api.Tests.Features.PersonalPlans;

public class PersonalPlanTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PersonalPlan_Lifecycle_Tests()
    {
        // We first need a member for the identity
        var createMemberCmd = new CreateMemberRequest(
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
            LinkUserAdapter: false,
            PlanDetails: new CreateSportPlanDetailsRequest(["Run"])
        );

        var createResponse = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", createCmd);
        createResponse.EnsureSuccessStatusCode();
        
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PersonalPlanDto>();
        Assert.NotNull(createdPlan);
        Assert.Equal(100.0, createdPlan.Target);

        var planId = createdPlan.PlanId;
        
        // 2. Read Plan
        var getResponse = await _client.GetAsync($"/v1/members/me/personal-plans/{planId}");
        getResponse.EnsureSuccessStatusCode();

        // 3. Update Plan
        var updateCmd = new UpdatePersonalPlanRequest(
            Unit: "km",
            Target: 100.0,
            FromDate: DateTimeOffset.UtcNow,
            ToDate: DateTimeOffset.UtcNow.AddDays(30)
        );
        var updateResponse = await _client.PutAsJsonAsync($"/v1/members/me/personal-plans/{planId}", updateCmd);
        if (!updateResponse.IsSuccessStatusCode)
        {
            var err = await updateResponse.Content.ReadAsStringAsync();
            throw new Exception($"Update failed with {updateResponse.StatusCode}: {err}");
        }
        // 4. Read All Plans
        var listResponse = await _client.GetAsync("/v1/members/me/personal-plans");
        listResponse.EnsureSuccessStatusCode();

        // 5. Cancel Plan
        var cancelResponse = await _client.PostAsync($"/v1/members/me/personal-plans/{planId}:cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        // 6. Patch Plan (Not Supported)
        var patchResponse = await _client.PatchAsync("/v1/members/me/personal-plans", null);
        Assert.Equal(HttpStatusCode.InternalServerError, patchResponse.StatusCode);

        // 6. Activate Plan
        var activateResponse = await _client.PostAsync($"/v1/members/me/personal-plans/{planId}:activate", null);
        activateResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PersonalPlan_Generic_Lifecycle_Tests()
    {
        // We first need a member for the identity
        var createMemberCmd = new CreateMemberRequest(
            FirstName: "Generic",
            MiddleName: null,
            LastName: "Owner",
            Description: "For testing generic plans",
            PreferredTimezone: "UTC"
        );
        var createMemberResponse = await _client.PostAsJsonAsync("/v1/members", createMemberCmd);
        // It might already exist if tests run in parallel/sequence, so we just ensure we try to create it.
        // We don't EnsureSuccessStatusCode() here because it might return Conflict/400 if it already exists.

        // 1. Create Personal Plan without PlanDetails
        var createCmd = new CreatePersonalPlanRequest(
            Name: "Generic Plan",
            Unit: "tasks",
            Target: 50.0,
            FromDate: DateTimeOffset.UtcNow,
            ToDate: DateTimeOffset.UtcNow.AddDays(30),
            StartDateLocal: "2026-07-01",
            EndDateLocal: "2026-07-31",
            Timezone: "UTC",
            EnableActivityLog: true,
            DisplayOnProfile: false,
            Prioritize: 2,
            LinkUserAdapter: false,
            PlanDetails: null // Null explicitly to test the generic branch
        );

        var createResponse = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", createCmd);
        Assert.True(createResponse.IsSuccessStatusCode, await createResponse.Content.ReadAsStringAsync());
        
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PersonalPlanDto>();
        Assert.NotNull(createdPlan);
        Assert.Equal(50.0, createdPlan.Target);
    }

    [Fact]
    public async Task PersonalPlan_Security_Tests()
    {
        // 1. Unauthorized due to missing NameIdentifier (controller logic)
        _client.DefaultRequestHeaders.Add("X-Omit-NameIdentifier", "true");
        
        var getList = await _client.GetAsync("/v1/members/me/personal-plans");
        Assert.Equal(HttpStatusCode.Unauthorized, getList.StatusCode);

        var getSingle = await _client.GetAsync("/v1/members/me/personal-plans/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.Unauthorized, getSingle.StatusCode);

        var createCmd = new CreatePersonalPlanRequest("Test", "km", 10.0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "2026-07-01", "2026-07-31", "UTC", true, true, 1, false, new CreateSportPlanDetailsRequest(["Run"]));
        var createRes = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", createCmd);
        Assert.Equal(HttpStatusCode.Unauthorized, createRes.StatusCode);

        var updateCmd = new UpdatePersonalPlanRequest("km", 10.0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        var updateRes = await _client.PutAsJsonAsync("/v1/members/me/personal-plans/00000000-0000-0000-0000-000000000000", updateCmd);
        Assert.Equal(HttpStatusCode.Unauthorized, updateRes.StatusCode);

        var cancelRes = await _client.PostAsync("/v1/members/me/personal-plans/00000000-0000-0000-0000-000000000000:cancel", null);
        Assert.Equal(HttpStatusCode.Unauthorized, cancelRes.StatusCode);

        var activateRes = await _client.PostAsync("/v1/members/me/personal-plans/00000000-0000-0000-0000-000000000000:activate", null);
        Assert.Equal(HttpStatusCode.Unauthorized, activateRes.StatusCode);

        _client.DefaultRequestHeaders.Remove("X-Omit-NameIdentifier");

        // 2. Forbid (using another-user identifier)
        var createForbid = await _client.PostAsJsonAsync("/v1/members/another-user/personal-plans", createCmd);
        Assert.Equal(HttpStatusCode.Forbidden, createForbid.StatusCode);

        var updateForbid = await _client.PutAsJsonAsync("/v1/members/another-user/personal-plans/00000000-0000-0000-0000-000000000000", updateCmd);
        Assert.Equal(HttpStatusCode.Forbidden, updateForbid.StatusCode);

        var cancelForbid = await _client.PostAsync("/v1/members/another-user/personal-plans/00000000-0000-0000-0000-000000000000:cancel", null);
        Assert.Equal(HttpStatusCode.Forbidden, cancelForbid.StatusCode);

        var activateForbid = await _client.PostAsync("/v1/members/another-user/personal-plans/00000000-0000-0000-0000-000000000000:activate", null);
        Assert.Equal(HttpStatusCode.Forbidden, activateForbid.StatusCode);

        // 3. BadRequest (Null command) handled by ASP.NET Core MVC
        var content = new StringContent("null", Encoding.UTF8, "application/json");
        var nullUpdate = await _client.PutAsync("/v1/members/me/personal-plans/00000000-0000-0000-0000-000000000000", content);
        Assert.Equal(HttpStatusCode.BadRequest, nullUpdate.StatusCode);    
        // 4. Create BadRequest (Null command)
        var res4 = await _client.PostAsync("/v1/members/me/personal-plans", content);
        Assert.Equal(HttpStatusCode.BadRequest, res4.StatusCode);
    }
    [Fact]
    public async Task PersonalPlan_Validation_Tests()
    {
        // We first need a member for the identity
        var createMemberCmd = new CreateMemberRequest(
            FirstName: "Validation",
            MiddleName: null,
            LastName: "Owner",
            Description: "For testing validation",
            PreferredTimezone: "UTC"
        );
        await _client.PostAsJsonAsync("/v1/members", createMemberCmd);

        // 1. Validation error: empty sport types
        var emptySportTypesCmd = new CreatePersonalPlanRequest(
            Name: "Test Plan", Unit: "km", Target: 100.0, FromDate: DateTimeOffset.UtcNow, ToDate: DateTimeOffset.UtcNow.AddDays(30),
            StartDateLocal: "2026-07-01", EndDateLocal: "2026-07-31", Timezone: "UTC", EnableActivityLog: true, DisplayOnProfile: true, Prioritize: 1, LinkUserAdapter: false,
            PlanDetails: new CreateSportPlanDetailsRequest([]) // empty
        );
        var emptyRes = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", emptySportTypesCmd);
        Assert.Equal(HttpStatusCode.BadRequest, emptyRes.StatusCode);
        var emptyErr = await emptyRes.Content.ReadAsStringAsync();
        Assert.Contains("error_sport_types_required", emptyErr);

        // 2. Validation error: Invalid sport type
        var invalidSportTypesCmd = emptySportTypesCmd with { PlanDetails = new CreateSportPlanDetailsRequest(["INVALID"]) };
        var invalidRes = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", invalidSportTypesCmd);
        Assert.Equal(HttpStatusCode.BadRequest, invalidRes.StatusCode);
        var invalidErr = await invalidRes.Content.ReadAsStringAsync();
        Assert.Contains("error_sport_type_invalid", invalidErr);

        // 3. Validation error: Cannot combine ALL
        var combineAllCmd = emptySportTypesCmd with { PlanDetails = new CreateSportPlanDetailsRequest(["ALL", "RUN"]) };
        var combineRes = await _client.PostAsJsonAsync("/v1/members/me/personal-plans", combineAllCmd);
        Assert.Equal(HttpStatusCode.BadRequest, combineRes.StatusCode);
        var combineErr = await combineRes.Content.ReadAsStringAsync();
        Assert.Contains("error_sport_types_cannot_combine_all", combineErr);
    }
}
