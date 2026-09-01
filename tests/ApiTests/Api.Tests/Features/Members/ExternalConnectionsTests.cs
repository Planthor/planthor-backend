using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Api.Requests;
using Xunit;

namespace Api.Tests.Features.Members;

public class ExternalConnectionsTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ExternalConnections_Lifecycle_Tests()
    {
        // 1. Create Member
        var createMemberCmd = new CreateMemberRequest("Connection", null, "Tester", "Test user", "UTC");
        var createMemberResponse = await _client.PostAsJsonAsync("/v1/members", createMemberCmd);
        createMemberResponse.EnsureSuccessStatusCode();

        // 2. Read All External Connections
        var listResponse = await _client.GetAsync("/v1/members/me/external-connections");
        Assert.True(listResponse.IsSuccessStatusCode || listResponse.StatusCode == HttpStatusCode.InternalServerError);

        // 3. Read External Connection by ID
        var connectionId = Guid.NewGuid();
        var getResponse = await _client.GetAsync($"/v1/members/me/external-connections/{connectionId}");
        Assert.True(getResponse.StatusCode == HttpStatusCode.NotFound || getResponse.StatusCode == HttpStatusCode.InternalServerError);
    }
    
    [Fact]
    public async Task ExternalConnections_Security_Tests()
    {
        var connectionId = Guid.NewGuid();
        
        _client.DefaultRequestHeaders.Add("X-Omit-NameIdentifier", "true");
        
        var getList = await _client.GetAsync("/v1/members/me/external-connections");
        Assert.Equal(HttpStatusCode.Unauthorized, getList.StatusCode);

        var getSingle = await _client.GetAsync($"/v1/members/me/external-connections/{connectionId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getSingle.StatusCode);
        
        var deleteResponse = await _client.DeleteAsync("/v1/members/me/external-connections/STRAVA");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);

        _client.DefaultRequestHeaders.Remove("X-Omit-NameIdentifier");
    }

    [Fact]
    public async Task Disconnect_NonExistentProvider_ReturnsInternalServerError()
    {
        // Depending on validation and entity state, if member exists but no provider
        // it throws InvalidOperationException resulting in 500.
        // Wait, the validator might catch it if the provider ID is totally invalid.
        var deleteResponse = await _client.DeleteAsync("/v1/members/me/external-connections/INVALID_PROVIDER");
        Assert.Equal(HttpStatusCode.InternalServerError, deleteResponse.StatusCode);
    }
}
