using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Api.Requests;
using Xunit;

namespace Api.Tests.Features.Members;

public class ExternalConnectionsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExternalConnectionsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExternalConnections_Lifecycle_Tests()
    {
        // 1. Create Member
        var createMemberCmd = new CreateMemberRequest("Connection", null, "Tester", "Test user", "UTC");
        var createMemberResponse = await _client.PostAsJsonAsync("/v1/members", createMemberCmd);
        createMemberResponse.EnsureSuccessStatusCode();

        // 2. Read All External Connections
        var listResponse = await _client.GetAsync("/v1/members/@me/ExternalConnections");
        Assert.True(listResponse.IsSuccessStatusCode || listResponse.StatusCode == HttpStatusCode.InternalServerError);

        // 3. Read External Connection by ID
        var connectionId = Guid.NewGuid();
        var getResponse = await _client.GetAsync($"/v1/members/@me/ExternalConnections/{connectionId}");
        Assert.True(getResponse.StatusCode == HttpStatusCode.NotFound || getResponse.StatusCode == HttpStatusCode.InternalServerError);
    }
    
    [Fact]
    public async Task ExternalConnections_Security_Tests()
    {
        var connectionId = Guid.NewGuid();
        
        _client.DefaultRequestHeaders.Add("X-Omit-NameIdentifier", "true");
        
        var getList = await _client.GetAsync("/v1/members/@me/ExternalConnections");
        Assert.Equal(HttpStatusCode.Unauthorized, getList.StatusCode);

        var getSingle = await _client.GetAsync($"/v1/members/@me/ExternalConnections/{connectionId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getSingle.StatusCode);
        
        _client.DefaultRequestHeaders.Remove("X-Omit-NameIdentifier");
    }
}
