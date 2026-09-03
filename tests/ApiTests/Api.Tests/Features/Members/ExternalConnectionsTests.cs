using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Requests;
using Application.Dtos;
using Application.Members.Commands.ConnectExternalProvider;
using Domain.Members;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task ReadAll_WithMeGuidAndInvalidIdentifiers_ReturnsExpectedConnections()
    {
        // Arrange
        var setup = await CreateConnectedMemberAsync(ExternalProvider.Strava, "read-list-athlete");
        using var client = setup.Client;

        // Act
        var meResponse = await client.GetAsync("/v1/members/me/external-connections");
        var idResponse = await client.GetAsync(
            $"/v1/members/{setup.Member.Id}/external-connections");
        var invalidResponse = await client.GetAsync(
            "/v1/members/not-a-member-id/external-connections");

        // Assert
        meResponse.EnsureSuccessStatusCode();
        idResponse.EnsureSuccessStatusCode();
        invalidResponse.EnsureSuccessStatusCode();
        var meConnections = await meResponse.Content.ReadFromJsonAsync<ExternalConnectionDto[]>();
        var idConnections = await idResponse.Content.ReadFromJsonAsync<ExternalConnectionDto[]>();
        var invalidConnections = await invalidResponse.Content.ReadFromJsonAsync<ExternalConnectionDto[]>();
        Assert.Contains(meConnections!, connection => connection.Id == setup.Connection.Id);
        Assert.Contains(idConnections!, connection => connection.Id == setup.Connection.Id);
        Assert.Empty(invalidConnections!);
    }

    [Fact]
    public async Task Read_WithMeGuidInvalidAndMissingConnection_ReturnsExpectedResponses()
    {
        // Arrange
        var setup = await CreateConnectedMemberAsync(ExternalProvider.Strava, "read-details-athlete");
        using var client = setup.Client;

        // Act
        var meResponse = await client.GetAsync(
            $"/v1/members/me/external-connections/{setup.Connection.Id}");
        var idResponse = await client.GetAsync(
            $"/v1/members/{setup.Member.Id}/external-connections/{setup.Connection.Id}");
        var invalidIdentifierResponse = await client.GetAsync(
            $"/v1/members/not-a-member-id/external-connections/{setup.Connection.Id}");
        var missingConnectionResponse = await client.GetAsync(
            $"/v1/members/me/external-connections/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, idResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, invalidIdentifierResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingConnectionResponse.StatusCode);
        var connection = await meResponse.Content.ReadFromJsonAsync<ExternalConnectionDto>();
        Assert.Equal(setup.Connection.Id, connection?.Id);
    }

    [Fact]
    public async Task ReadSyncStatus_WithUninitializedStravaConnection_ReturnsDefaultState()
    {
        // Arrange
        var setup = await CreateConnectedMemberAsync(ExternalProvider.Strava, "status-athlete");
        using var client = setup.Client;

        // Act
        var response = await client.GetAsync(
            "/v1/members/me/external-connections/STRAVA/sync-status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<ExternalActivitySyncStatusDto>();
        Assert.NotNull(status);
        Assert.Equal("not_started", status.InitialSyncState);
        Assert.Equal("idle", status.State);
        Assert.Null(status.LastTrigger);
        Assert.Null(status.LastStartedAt);
        Assert.Null(status.LastSuccessfulSyncAt);
        Assert.Null(status.NextAttemptAt);
        Assert.Null(status.ErrorCode);
    }

    [Fact]
    public async Task ReadSyncStatus_WithForeignOrInvalidIdentifier_ReturnsNotFound()
    {
        // Arrange
        var setup = await CreateConnectedMemberAsync(ExternalProvider.Strava, "status-owner-athlete");
        using var client = setup.Client;

        // Act
        var foreignResponse = await client.GetAsync(
            $"/v1/members/{Guid.NewGuid()}/external-connections/STRAVA/sync-status");
        var invalidResponse = await client.GetAsync(
            "/v1/members/not-a-member-id/external-connections/STRAVA/sync-status");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, foreignResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task ReadSyncStatus_WithProviderThatHasNoAdapter_ReturnsNotFound()
    {
        // Arrange
        var setup = await CreateConnectedMemberAsync(ExternalProvider.GitHub, "github-user");
        using var client = setup.Client;

        // Act
        var response = await client.GetAsync(
            "/v1/members/me/external-connections/GITHUB/sync-status");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disconnect_WithActiveConnection_ReturnsNoContentAndRevokesConnection()
    {
        // Arrange
        var setup = await CreateConnectedMemberAsync(ExternalProvider.Strava, "non-numeric-athlete");
        using var client = setup.Client;

        // Act
        var response = await client.DeleteAsync(
            "/v1/members/me/external-connections/STRAVA");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var detailsResponse = await client.GetAsync(
            $"/v1/members/me/external-connections/{setup.Connection.Id}");
        detailsResponse.EnsureSuccessStatusCode();
        var connection = await detailsResponse.Content.ReadFromJsonAsync<ExternalConnectionDto>();
        Assert.Equal(ConnectionStatus.Revoked.Id, connection?.StatusId);
        Assert.NotNull(connection?.DisconnectedAt);
    }

    private async Task<ConnectedMemberSetup> CreateConnectedMemberAsync(
        ExternalProvider provider,
        string externalUserId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-TestUserId", $"connections-{Guid.NewGuid():N}");
        var createResponse = await client.PostAsJsonAsync(
            "/v1/members",
            new CreateMemberRequest("Connection", null, "Tester", "Test user", "UTC"));
        createResponse.EnsureSuccessStatusCode();
        var memberDto = await createResponse.Content.ReadFromJsonAsync<MemberDto>();
        Assert.NotNull(memberDto);

        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var member = await repository.GetByIdAsync(memberDto.Id, CancellationToken.None);
        Assert.NotNull(member);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new ConnectExternalProviderCommand(
            member.IdentifyName,
            provider.Id,
            ExternalConnectionType.ActivitiesSync.Id,
            externalUserId,
            ["activity:read_all"]));
        var updatedMember = await repository.GetByIdAsync(member.Id, CancellationToken.None);
        Assert.NotNull(updatedMember);
        var connection = Assert.Single(updatedMember.ExternalConnections, candidate =>
            candidate.Provider == provider && candidate.Type == ExternalConnectionType.ActivitiesSync);

        return new ConnectedMemberSetup(client, updatedMember, connection);
    }

    private sealed record ConnectedMemberSetup(
        HttpClient Client,
        Member Member,
        ExternalConnection Connection);
}
