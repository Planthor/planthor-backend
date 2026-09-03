using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace Api.Tests.Features.Members;

// Use a class fixture to share the factory (and Testcontainers/Wiremock) across tests
public class ProvisionMemberTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ProvisionMemberTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        
        // Reset Wiremock before each test so stubs don't leak between tests
        _factory.WireMockServer.Reset(); 
    }

    [Fact]
    public async Task GetMembers_WhenUserIsNew_ShouldProvisionMemberAndTriggerAvatarDownload()
    {
        // Arrange
        var subjectId = $"keycloak-id-{System.Guid.NewGuid():N}";
        var avatarPath = $"/facebook/{subjectId}/avatar.jpg";
        var avatarStorage = new RecordingAvatarStorageService();
        using var testFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAvatarStorageService>();
                services.AddSingleton<IAvatarStorageService>(avatarStorage);
            }));
        using var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-TestUserId", subjectId);
        client.DefaultRequestHeaders.Add("X-TestPreferredUsername", "test.user@example.com");
        client.DefaultRequestHeaders.Add("X-TestGivenName", "Test");
        client.DefaultRequestHeaders.Add("X-TestSurname", "User");
        client.DefaultRequestHeaders.Add("X-TestAvatarUrl", $"{_factory.WireMockServer.Url}{avatarPath}");

        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath("/realms/planthor/protocol/openid-connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{ \"access_token\": \"keycloak-admin-token\" }"));

        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/admin/realms/planthor/users/{subjectId}/federated-identity")
                .UsingGet()
                .WithHeader("Authorization", "Bearer keycloak-admin-token"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    [
                      { "identityProvider": "FACEBOOK", "userId": "facebook-user", "userName": "test.user" },
                      { "identityProvider": "UNKNOWN", "userId": "unknown-user", "userName": "unknown" }
                    ]
                    """));

        _factory.WireMockServer
            .Given(Request.Create().WithPath(avatarPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "image/jpeg")
                .WithBody(new byte[] { 0xFF, 0xD8, 0xFF }));

        // Act
        var response = await client.GetAsync($"/v1/members/{System.Guid.NewGuid()}");
        var provisionedMember = await WaitForProvisioningAsync(testFactory.Services, subjectId);
        var secondResponse = await client.GetAsync($"/v1/members/{System.Guid.NewGuid()}");
        await WaitForRequestCountAsync(
            $"/admin/realms/planthor/users/{subjectId}/federated-identity",
            expectedCount: 2);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);
        Assert.Equal("JIT Provisioned", provisionedMember.Description);
        Assert.StartsWith("TEST.USER_", provisionedMember.IdentifyName, System.StringComparison.Ordinal);
        Assert.Equal("https://cdn.planthor.test/avatar.jpg", provisionedMember.PathAvatar);
        Assert.Contains(provisionedMember.ExternalConnections, connection =>
            connection.Provider == ExternalProvider.Facebook &&
            connection.Type == ExternalConnectionType.Identity &&
            connection.ExternalUserId == "facebook-user");
        Assert.Equal("image/jpeg", avatarStorage.ContentType);
        Assert.Equal(3, avatarStorage.BytesUploaded);
    }

    private async Task<Member> WaitForProvisioningAsync(
        System.IServiceProvider services,
        string subjectId)
    {
        for (var attempt = 0; attempt < 300; attempt++)
        {
            await using var scope = services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
            var member = await repository.GetByExternalIdentityAsync(
                ExternalProvider.Keycloak.Id,
                subjectId,
                CancellationToken.None);
            if (member?.PathAvatar == "https://cdn.planthor.test/avatar.jpg" &&
                member.ExternalConnections.Any(connection =>
                    connection.Provider == ExternalProvider.Facebook &&
                    connection.Type == ExternalConnectionType.Identity))
            {
                return member;
            }

            await Task.Delay(100);
        }

        throw new System.TimeoutException("Member provisioning background jobs did not complete.");
    }

    private async Task WaitForRequestCountAsync(string path, int expectedCount)
    {
        for (var attempt = 0; attempt < 300; attempt++)
        {
            var count = _factory.WireMockServer.LogEntries.Count(entry =>
                entry.RequestMessage?.Path == path);
            if (count >= expectedCount)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new System.TimeoutException(
            $"Keycloak endpoint '{path}' did not receive {expectedCount} requests.");
    }

    private sealed class RecordingAvatarStorageService : IAvatarStorageService
    {
        public string? ContentType { get; private set; }

        public int BytesUploaded { get; private set; }

        public Task DeleteAvatarAsync(System.Uri blobUri, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public async Task<string> UploadAvatarAsync(
            System.Guid memberId,
            Stream fileStream,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await fileStream.CopyToAsync(buffer, cancellationToken);
            ContentType = contentType;
            BytesUploaded = (int)buffer.Length;
            return "https://cdn.planthor.test/avatar.jpg";
        }
    }
}
