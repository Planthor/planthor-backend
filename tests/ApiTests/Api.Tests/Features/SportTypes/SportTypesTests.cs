using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Api.Responses;
using Xunit;

namespace Api.Tests.Features.SportTypes;

public class SportTypesTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SportTypesTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSportTypes_ReturnsListOfAvailableTypes()
    {
        var response = await _client.GetAsync("/v1/sport-types");
        
        response.EnsureSuccessStatusCode();
        var types = await response.Content.ReadFromJsonAsync<List<SportTypeResponse>>();
        
        Assert.NotNull(types);
        Assert.NotEmpty(types);
        
        // Assert that specific types exist
        Assert.Contains(types, t => t.Id == "ALL");
        Assert.Contains(types, t => t.Id == "RUN");
        Assert.Contains(types, t => t.Id == "SWIM");
        Assert.Contains(types, t => t.Id == "WALK");
        Assert.Contains(types, t => t.Id == "HIKE");
        Assert.Contains(types, t => t.Id == "RIDE");
    }
}
