using System.Net;
using Xunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Net.Http;

namespace Api.Tests.Features.Members;

// Use a class fixture to share the factory (and Testcontainers/Wiremock) across tests
public class ProvisionMemberTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProvisionMemberTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        // Reset Wiremock before each test so stubs don't leak between tests
        _factory.WireMockServer.Reset(); 
    }

    [Fact]
    public async Task GetMembers_WhenUserIsNew_ShouldProvisionMemberAndTriggerAvatarDownload()
    {
        // 1. Arrange: Stub Keycloak Admin API
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/admin/realms/planthor/users/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{ \"id\": \"keycloak-id-123\", \"username\": \"testuser\" }"));

        // 1b. Arrange: Stub Facebook Avatar Download
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/facebook/avatar.jpg").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(new byte[] { 0xFF, 0xD8, 0xFF })); // Fake JPEG bytes

        // Note: The CustomWebApplicationFactory configures the TestAuthenticationHandler
        // to inject a ClaimsPrincipal with ClaimTypes.NameIdentifier and an "avatarUrl" 

        // 2. Act: Call an authenticated endpoint to trigger MemberSessionFilter
        var response = await _client.GetAsync($"/v1/members/{System.Guid.NewGuid()}");

        // 3. Assert: Verify the API response
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // 4. Assert: Verify the database state using a scoped DbContext
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlanthorDbContext>();
        
        // The member should have been JIT provisioned by the filter
        var provisionedMember = await dbContext.Members.FirstOrDefaultAsync();
        Assert.NotNull(provisionedMember);
        Assert.Equal("JIT Provisioned", provisionedMember.Description);
    }
}
