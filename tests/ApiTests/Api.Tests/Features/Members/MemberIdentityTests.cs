using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Requests;
using Application.Shared;
using Domain.Members;
using Infrastructure.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Features.Members;

public sealed class MemberIdentityTests : IClassFixture<MemberIdentityFactory>, IDisposable
{
    private readonly MemberIdentityFactory _factory;
    private readonly HttpClient _client;
    private readonly IBackgroundJobClient _jobs;
    private readonly string _subjectId = $"identity-{Guid.NewGuid():N}";

    public MemberIdentityTests(MemberIdentityFactory factory)
    {
        _factory = factory;
        _jobs = factory.Jobs;
        _jobs.ClearReceivedCalls();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-TestUserId", _subjectId);
    }

    public static TheoryData<string> Usernames => new()
    {
        "ordinary_username",
        "MixedCase.Username",
        "Case.Preserved+training@Example.com",
        $"{new string('u', 64)}@{new string('d', 60)}.example.com"
    };

    [Fact]
    public async Task OpenApi_MemberPatchContract_ExcludesIdentifyName()
    {
        // Arrange
        const string path = "/openapi/v1.json";

        // Act
        var response = await _client.GetAsync(path);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var properties = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty(nameof(PatchMemberRequest)).GetProperty("properties");
        Assert.False(properties.TryGetProperty("identifyName", out _));
        Assert.True(properties.TryGetProperty("updateMask", out _));
        Assert.True(properties.TryGetProperty("firstName", out _));
        Assert.True(properties.TryGetProperty("lastName", out _));
        Assert.True(properties.TryGetProperty("autoLinkUserAdapterToPlan", out _));
    }

    [Theory]
    [MemberData(nameof(Usernames))]
    public async Task GetMembers_WithPreferredUsername_PersistsExactValueAndReusesMember(string username)
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-TestPreferredUsername", username);

        // Act
        var firstResponse = await _client.GetAsync("/v1/members");
        var firstMember = await GetMemberAsync();
        var secondResponse = await _client.GetAsync("/v1/members");
        var secondMember = await GetMemberAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(firstMember);
        Assert.NotNull(secondMember);
        Assert.Equal(username, secondMember.IdentifyName);
        Assert.Equal(firstMember.Id, secondMember.Id);
        var connection = Assert.Single(secondMember.ExternalConnections);
        Assert.Equal(ExternalProvider.Keycloak, connection.Provider);
        Assert.Equal(_subjectId, connection.ExternalUserId);
        Assert.Equal(1, await CountMembersAsync());
        await _jobs.Received(2).EnqueueIdentitySyncAsync(
            secondMember.Id, username, Arg.Any<CancellationToken>());
        await _jobs.DidNotReceiveWithAnyArgs().EnqueueAvatarDownloadAsync(default, default!, default);
    }

    [Fact]
    public async Task GetMembers_WithSharedEmailLocalPart_ProvisionsDistinctExactNames()
    {
        // Arrange
        var localPart = $"shared-{Guid.NewGuid():N}";
        _client.DefaultRequestHeaders.Add("X-TestPreferredUsername", $"{localPart}@one.example");
        using var secondClient = _factory.CreateClient();
        var secondSubject = $"identity-{Guid.NewGuid():N}";
        secondClient.DefaultRequestHeaders.Add("X-TestUserId", secondSubject);
        secondClient.DefaultRequestHeaders.Add("X-TestPreferredUsername", $"{localPart}@two.example");

        // Act
        var firstResponse = await _client.GetAsync("/v1/members");
        var secondResponse = await secondClient.GetAsync("/v1/members");

        // Assert
        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();
        var first = await GetMemberAsync();
        var second = await GetMemberAsync(secondSubject);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal($"{localPart}@one.example", first.IdentifyName);
        Assert.Equal($"{localPart}@two.example", second.IdentifyName);
    }

    [Fact]
    public async Task GetMembers_WithChangedClaim_KeepsStoredNameAndSubjectConnection()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-TestPreferredUsername", "Original.Name@example.com");
        (await _client.GetAsync("/v1/members")).EnsureSuccessStatusCode();
        var original = await GetMemberAsync();
        Assert.NotNull(original);
        _client.DefaultRequestHeaders.Remove("X-TestPreferredUsername");
        _client.DefaultRequestHeaders.Add("X-TestPreferredUsername", "Changed.Name@example.com");
        _jobs.ClearReceivedCalls();

        // Act
        var response = await _client.GetAsync("/v1/members/me/personal-plans");

        // Assert
        response.EnsureSuccessStatusCode();
        var member = await GetMemberAsync();
        Assert.NotNull(member);
        Assert.Equal(original.Id, member.Id);
        Assert.Equal(original.IdentifyName, member.IdentifyName);
        Assert.Equal(1, await CountMembersAsync());
        await _jobs.Received(1).EnqueueIdentitySyncAsync(
            original.Id, original.IdentifyName, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public async Task GetMembers_WithInvalidUsername_ReturnsUnauthorizedWithoutSideEffects(
        string? username, bool existingMember)
    {
        // Arrange
        if (existingMember)
        {
            (await _client.GetAsync("/v1/members")).EnsureSuccessStatusCode();
        }
        var original = await GetMemberAsync();
        _jobs.ClearReceivedCalls();
        if (username is null)
        {
            _client.DefaultRequestHeaders.Add("X-Omit-PreferredUsername", "true");
        }
        else if (username.Length == 0)
        {
            _client.DefaultRequestHeaders.Add("X-Empty-PreferredUsername", "true");
        }
        else
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation("X-TestPreferredUsername", username);
        }
        _client.DefaultRequestHeaders.Add("X-TestGivenName", "NoFallback");
        _client.DefaultRequestHeaders.Add("X-TestAvatarUrl", "https://avatar.example/image.jpg");

        // Act
        var response = await _client.GetAsync("/v1/members");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var member = await GetMemberAsync();
        Assert.Equal(original?.Id, member?.Id);
        Assert.Equal(original?.IdentifyName, member?.IdentifyName);
        Assert.Equal(existingMember ? 1 : 0, await CountMembersAsync());
        Assert.Empty(_jobs.ReceivedCalls());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMembers_WithInvalidSubject_ReturnsUnauthorizedWithoutProvisioning(string? subject)
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("X-TestUserId");
        _client.DefaultRequestHeaders.Add("X-TestPreferredUsername", $"valid-{_subjectId}");
        if (subject is null)
        {
            _client.DefaultRequestHeaders.Add("X-Omit-NameIdentifier", "true");
        }
        else if (subject.Length == 0)
        {
            _client.DefaultRequestHeaders.Add("X-Empty-NameIdentifier", "true");
        }
        else
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation("X-TestUserId", subject);
        }

        // Act
        var response = await _client.GetAsync("/v1/members");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        Assert.Null(await repository.GetByIdentifyNameAsync($"valid-{_subjectId}", CancellationToken.None));
        Assert.Empty(_jobs.ReceivedCalls());
    }

    [Theory]
    [InlineData("IdentifyName", false)]
    [InlineData("identifyname", true)]
    [InlineData("IDENTIFYNAME", true)]
    public async Task PatchMember_WithRenameMask_ReturnsValidationErrorWithoutPartialUpdate(
        string field, bool includeProfile)
    {
        // Arrange
        (await _client.GetAsync("/v1/members")).EnsureSuccessStatusCode();
        var original = await GetMemberAsync();
        Assert.NotNull(original);
        var payload = new
        {
            updateMask = includeProfile ? new[] { "FirstName", field } : new[] { field },
            identifyName = "client-chosen-name",
            firstName = "MustNotBeSaved"
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/v1/members/{original.Id}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(problem.RootElement.GetProperty("errors").EnumerateArray(),
            error => error.GetProperty("code").GetString() == "error_identify_name_read_only_should_be_updated_only_by_the_idp");
        var member = await GetMemberAsync();
        Assert.NotNull(member);
        Assert.Equal(original.IdentifyName, member.IdentifyName);
        Assert.Equal(original.FirstName, member.FirstName);
    }

    [Theory]
    [InlineData("FirstName", "Profile", null, null)]
    [InlineData("LastName", null, "Updated", null)]
    [InlineData("AutoLinkUserAdapterToPlan", null, null, true)]
    [InlineData("AutoLinkUserAdapterToPlan", null, null, false)]
    [InlineData("AutoLinkUserAdapterToPlan", null, null, null)]
    public async Task PatchMember_WithEditableField_PreservesIdentity(
        string field, string? firstName, string? lastName, bool? autoLink)
    {
        // Arrange
        (await _client.GetAsync("/v1/members")).EnsureSuccessStatusCode();
        var original = await GetMemberAsync();
        Assert.NotNull(original);
        var request = new PatchMemberRequest([field], firstName, lastName, autoLink);

        // Act
        var response = await _client.PatchAsJsonAsync($"/v1/members/{original.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var member = await GetMemberAsync();
        Assert.NotNull(member);
        Assert.Equal(original.IdentifyName, member.IdentifyName);
        Assert.Equal(firstName ?? original.FirstName, member.FirstName);
        Assert.Equal(lastName ?? original.LastName, member.LastName);
        Assert.Equal(autoLink ?? original.AutoLinkUserAdapterToPlan, member.AutoLinkUserAdapterToPlan);
    }

    private async Task<Member?> GetMemberAsync(string? subjectId = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMemberRepository>()
            .GetByExternalIdentityAsync(ExternalProvider.Keycloak.Id, subjectId ?? _subjectId, CancellationToken.None);
    }

    private async Task<int> CountMembersAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PlanthorDbContext>().Members
            .CountAsync(member => member.ExternalConnections.Any(connection =>
                connection.Provider == ExternalProvider.Keycloak && connection.ExternalUserId == _subjectId));
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

public sealed class MemberIdentityFactory : CustomWebApplicationFactory<Program>
{
    public IBackgroundJobClient Jobs { get; } = Substitute.For<IBackgroundJobClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBackgroundJobClient>();
            services.AddSingleton(Jobs);
        });
    }
}
