using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Api.Requests;
using Application.Dtos;
using Application.Shared;
using Domain.Members;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Features.Members;

/// <summary>
/// Verifies member identifier routing and ownership through HTTP and real persistence.
/// </summary>
public sealed class MemberIdentifierTests(MemberIdentifierFactory factory)
    : IClassFixture<MemberIdentifierFactory>
{
    /// <summary>All supported ways of addressing the authenticated member.</summary>
    public static TheoryData<string> Identifiers => new() { "me", "ME", "mE", "guid", "name" };

    /// <summary>Every member operation must reject an invalid authentication context.</summary>
    public static TheoryData<string, string> UnauthorizedRequests
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var method in new[] { "GET", "PUT", "PATCH" })
            {
                foreach (var header in new[]
                {
                    "X-Force-Unauthorized", "X-Omit-NameIdentifier", "X-Omit-PreferredUsername",
                    "X-Empty-NameIdentifier", "X-Empty-PreferredUsername"
                })
                {
                    data.Add(method, header);
                }
            }
            return data;
        }
    }

    /// <summary>Reads via aliases, GUIDs and exact identity names reach the same profile.</summary>
    [Theory]
    [MemberData(nameof(Identifiers))]
    public async Task GetMember_WithSupportedIdentifier_ReturnsAuthenticatedMember(string kind)
    {
        using var client = CreateClient(out var identityName);
        var member = await ProvisionAsync(client);
        var identifier = Identifier(kind, member.Id, identityName);

        var response = await client.GetAsync($"/v1/members/{identifier}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(member, await response.Content.ReadFromJsonAsync<MemberDto>());
    }

    /// <summary>Full updates persist against the member selected by each identifier.</summary>
    [Theory]
    [MemberData(nameof(Identifiers))]
    public async Task PutMember_WithSupportedIdentifier_UpdatesAuthenticatedMember(string kind)
    {
        using var client = CreateClient(out var identityName);
        var original = await ProvisionAsync(client);
        var identifier = Identifier(kind, original.Id, identityName);
        var request = new UpdateMemberRequest("Updated", "Middle", "Profile", "Description",
            "https://example.com/avatar.png", "Asia/Ho_Chi_Minh", true);

        var response = await client.PutAsJsonAsync($"/v1/members/{identifier}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var member = await GetStoredMemberAsync(original.Id);
        Assert.Equal(request.FirstName, member.FirstName);
        Assert.Equal(request.MiddleName, member.MiddleName);
        Assert.Equal(request.LastName, member.LastName);
        Assert.Equal(request.Description, member.Description);
        Assert.Equal(request.PathAvatar, member.PathAvatar);
        Assert.Equal(request.PreferredTimezone, member.PreferredTimezone);
        Assert.True(member.AutoLinkUserAdapterToPlan);
        Assert.Equal(identityName, member.IdentifyName);
    }

    /// <summary>Field masks persist only selected fields for each supported identifier.</summary>
    [Theory]
    [MemberData(nameof(Identifiers))]
    public async Task PatchMember_WithSupportedIdentifier_UpdatesOnlyMaskedFields(string kind)
    {
        using var client = CreateClient(out var identityName);
        var original = await ProvisionAsync(client);
        var identifier = Identifier(kind, original.Id, identityName);
        var request = new PatchMemberRequest(["FirstName", "AutoLinkUserAdapterToPlan"],
            "Patched", "MustNotChange", true);

        var response = await client.PatchAsJsonAsync($"/v1/members/{identifier}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var member = await GetStoredMemberAsync(original.Id);
        Assert.Equal("Patched", member.FirstName);
        Assert.Equal(original.LastName, member.LastName);
        Assert.True(member.AutoLinkUserAdapterToPlan);
        Assert.Equal(identityName, member.IdentifyName);
    }

    /// <summary>A caller cannot update another member through either explicit identifier.</summary>
    [Theory]
    [InlineData("PUT", "guid")]
    [InlineData("PUT", "name")]
    [InlineData("PATCH", "guid")]
    [InlineData("PATCH", "name")]
    public async Task WriteMember_WithAnotherMembersIdentifier_ReturnsForbiddenWithoutChanges(
        string method, string kind)
    {
        using var client = CreateClient(out _);
        var current = await ProvisionAsync(client);
        using var otherClient = CreateClient(out var otherName);
        var other = await ProvisionAsync(otherClient);
        var identifier = Identifier(kind, other.Id, otherName);

        var response = await SendAsync(client, method, identifier);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(current, await client.GetFromJsonAsync<MemberDto>("/v1/members/me"));
        Assert.Equal(other, await otherClient.GetFromJsonAsync<MemberDto>("/v1/members/me"));
    }

    /// <summary>Authenticated reads of another profile remain supported.</summary>
    [Theory]
    [InlineData("guid")]
    [InlineData("name")]
    public async Task GetMember_WithAnotherMembersIdentifier_ReturnsRequestedMember(string kind)
    {
        using var client = CreateClient(out _);
        using var otherClient = CreateClient(out var otherName);
        var other = await ProvisionAsync(otherClient);

        var response = await client.GetAsync($"/v1/members/{Identifier(kind, other.Id, otherName)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(other, await response.Content.ReadFromJsonAsync<MemberDto>());
    }

    /// <summary>The alias follows the stored session even when the identity claim changes.</summary>
    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task MemberEndpoint_WithChangedUsernameClaim_UsesOriginalSessionMember(string method)
    {
        using var client = CreateClient(out var originalName);
        var original = await ProvisionAsync(client);
        client.DefaultRequestHeaders.Remove("X-TestPreferredUsername");
        client.DefaultRequestHeaders.Add("X-TestPreferredUsername", $"Changed-{Guid.NewGuid():N}");

        var response = await SendAsync(client, method, "me");

        Assert.Equal(method == "GET" ? HttpStatusCode.OK : HttpStatusCode.NoContent, response.StatusCode);
        var member = await GetStoredMemberAsync(original.Id);
        Assert.Equal(originalName, member.IdentifyName);
        var retrieved = await client.GetFromJsonAsync<MemberDto>("/v1/members/me");
        Assert.NotNull(retrieved);
        Assert.Equal(original.Id, retrieved.Id);
    }

    /// <summary>GUID-shaped identity names cannot redirect the alias to another member.</summary>
    [Fact]
    public async Task GetMember_WithGuidShapedIdentityName_ResolvesMeFromSessionId()
    {
        using var otherClient = CreateClient(out _);
        var other = await ProvisionAsync(otherClient);
        using var client = CreateClient(out _);
        client.DefaultRequestHeaders.Remove("X-TestPreferredUsername");
        client.DefaultRequestHeaders.Add("X-TestPreferredUsername", other.Id.ToString());

        var member = await client.GetFromJsonAsync<MemberDto>("/v1/members/me");

        Assert.NotNull(member);
        Assert.NotEqual(other.Id, member.Id);
        Assert.Equal(other.Id.ToString(), (await GetStoredMemberAsync(member.Id)).IdentifyName);
    }

    /// <summary>Unknown identity names produce a not-found response for all operations.</summary>
    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task MemberEndpoint_WithUnknownIdentityName_ReturnsNotFound(string method)
    {
        using var client = CreateClient(out _);

        var response = await SendAsync(client, method, $"missing-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>GUID reads retain existing not-found and validation responses.</summary>
    [Theory]
    [InlineData(false, HttpStatusCode.NotFound)]
    [InlineData(true, HttpStatusCode.BadRequest)]
    public async Task GetMember_WithInvalidMemberId_ReturnsExpectedError(bool empty, HttpStatusCode expected)
    {
        using var client = CreateClient(out _);
        var id = empty ? Guid.Empty : Guid.NewGuid();

        var response = await client.GetAsync($"/v1/members/{id}");

        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Authorization and session validation protect every alias operation.</summary>
    [Theory]
    [MemberData(nameof(UnauthorizedRequests))]
    public async Task MemberEndpoint_WithInvalidAuthentication_ReturnsUnauthorized(string method, string header)
    {
        using var client = CreateClient(out _);
        client.DefaultRequestHeaders.Add(header, "true");

        var response = await SendAsync(client, method, "me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Controllers fail closed if session data disappears after authentication.</summary>
    [Theory]
    [InlineData("GET", "member-missing")]
    [InlineData("GET", "member-invalid")]
    [InlineData("PUT", "member-missing")]
    [InlineData("PATCH", "member-missing")]
    [InlineData("POST", "name-missing")]
    [InlineData("POST", "name-invalid")]
    [InlineData("POST", "name-empty")]
    public async Task MemberEndpoint_WithUnavailableSession_ReturnsUnauthorized(string method, string session)
    {
        using var client = CreateClient(out _);
        client.DefaultRequestHeaders.Add("X-Test-InvalidMemberSession", session);

        var response = await SendAsync(client, method, "me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>An explicit GUID cannot bypass a missing ownership context.</summary>
    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task WriteMember_WithMissingSessionAndExplicitId_ReturnsForbiddenWithoutChanges(string method)
    {
        using var client = CreateClient(out _);
        var original = await ProvisionAsync(client);
        client.DefaultRequestHeaders.Add("X-Test-InvalidMemberSession", "member-missing");

        var response = await SendAsync(client, method, original.Id.ToString());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-InvalidMemberSession");
        Assert.Equal(original, await client.GetFromJsonAsync<MemberDto>("/v1/members/me"));
    }

    /// <summary>A first-request write provisions and updates only the authenticated member.</summary>
    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task WriteMember_WithNewSession_ProvisionsAndUpdatesOnlyCaller(string method)
    {
        using var client = CreateClient(out var identityName);
        using var otherClient = CreateClient(out _);
        var other = await ProvisionAsync(otherClient);

        var response = await SendAsync(client, method, "me");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var member = await client.GetFromJsonAsync<MemberDto>("/v1/members/me");
        Assert.NotNull(member);
        Assert.Equal("Changed", member.FirstName);
        Assert.Equal(identityName, (await GetStoredMemberAsync(member.Id)).IdentifyName);
        Assert.Equal(other, await otherClient.GetFromJsonAsync<MemberDto>("/v1/members/me"));
    }

    /// <summary>Only the reserved alias is case insensitive; identity names retain their case.</summary>
    [Fact]
    public async Task GetMember_WithDifferentCaseIdentityName_ReturnsNotFound()
    {
        using var client = CreateClient(out var identityName);
        await ProvisionAsync(client);

        var response = await client.GetAsync($"/v1/members/{Uri.EscapeDataString(identityName.ToUpperInvariant())}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Model binding still rejects absent and malformed request bodies.</summary>
    [Theory]
    [InlineData("PUT", "null")]
    [InlineData("PUT", "{")]
    [InlineData("PATCH", "null")]
    [InlineData("PATCH", "{")]
    public async Task WriteMember_WithInvalidBody_ReturnsBadRequest(string method, string json)
    {
        using var client = CreateClient(out _);
        var original = await ProvisionAsync(client);
        using var request = new HttpRequestMessage(new HttpMethod(method), "/v1/members/me")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(original, await client.GetFromJsonAsync<MemberDto>("/v1/members/me"));
    }

    /// <summary>Resolving the alias does not bypass application validation.</summary>
    [Theory]
    [InlineData("PUT", "{\"firstName\":\"\",\"lastName\":\"Valid\",\"preferredTimezone\":\"UTC\"}")]
    [InlineData("PUT", "{\"firstName\":\"Valid\",\"lastName\":\"Valid\",\"preferredTimezone\":\"invalid\"}")]
    [InlineData("PATCH", "{\"updateMask\":[]}")]
    [InlineData("PATCH", "{\"updateMask\":[\"IdentifyName\",\"FirstName\"],\"firstName\":\"Unsaved\"}")]
    public async Task WriteMember_WithInvalidFields_ReturnsBadRequestWithoutChanges(string method, string json)
    {
        using var client = CreateClient(out _);
        var original = await ProvisionAsync(client);
        using var request = new HttpRequestMessage(new HttpMethod(method), "/v1/members/me")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(original, await client.GetFromJsonAsync<MemberDto>("/v1/members/me"));
    }

    /// <summary>The generated contract advertises string identifiers and authorization errors.</summary>
    [Fact]
    public async Task OpenApi_WithMemberRoutes_DescribesFlexibleIdentifiers()
    {
        using var client = CreateClient(out _);

        var response = await client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var path = Assert.Single(document.RootElement.GetProperty("paths").EnumerateObject(),
            path => path.Name.Equals("/v1/members/{identifier}", StringComparison.OrdinalIgnoreCase)).Value;
        foreach (var method in new[] { "get", "put", "patch" })
        {
            var operation = path.GetProperty(method);
            var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray(),
                parameter => parameter.GetProperty("name").GetString() == "identifier");
            Assert.Equal("string", parameter.GetProperty("schema").GetProperty("type").GetString());
            Assert.True(operation.GetProperty("responses").TryGetProperty("401", out _));
        }
        Assert.True(path.GetProperty("put").GetProperty("responses").TryGetProperty("403", out _));
        Assert.True(path.GetProperty("patch").GetProperty("responses").TryGetProperty("403", out _));
    }

    private HttpClient CreateClient(out string identityName)
    {
        var subject = $"identifier-{Guid.NewGuid():N}";
        identityName = $"Mixed.Case+{subject}@Example.com";
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-TestUserId", subject);
        client.DefaultRequestHeaders.Add("X-TestPreferredUsername", identityName);
        return client;
    }

    private static async Task<MemberDto> ProvisionAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/v1/members",
            new CreateMemberRequest("Original", null, "Member", null, "UTC", false));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var member = await response.Content.ReadFromJsonAsync<MemberDto>();
        Assert.NotNull(member);
        return member;
    }

    private async Task<Member> GetStoredMemberAsync(Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var member = await scope.ServiceProvider.GetRequiredService<IMemberRepository>()
            .GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(member);
        return member;
    }

    private static string Identifier(string kind, Guid id, string identityName) => kind switch
    {
        "guid" => id.ToString(),
        "name" => Uri.EscapeDataString(identityName),
        _ => kind
    };

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string identifier)
    {
        var path = $"/v1/members/{identifier}";
        return method switch
        {
            "GET" => client.GetAsync(path),
            "PUT" => client.PutAsJsonAsync(path,
                new UpdateMemberRequest("Changed", null, "Profile", null, null, "UTC", true)),
            "PATCH" => client.PatchAsJsonAsync(path,
                new PatchMemberRequest(["FirstName"], "Changed", null, null)),
            "POST" => client.PostAsJsonAsync("/v1/members",
                new CreateMemberRequest("Created", null, "Profile", null, "UTC", false)),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }
}

/// <summary>
/// Runs the real member pipeline with isolated databases and optional session fault injection.
/// </summary>
public sealed class MemberIdentifierFactory : CustomWebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBackgroundJobClient>();
            services.AddSingleton(Substitute.For<IBackgroundJobClient>());
            services.Configure<MvcOptions>(options => options.Filters.Add(new InvalidMemberSessionFilter()));
        });
    }

    // Runs after MemberSessionFilter to verify defensive handling at the HTTP boundary.
    private sealed class InvalidMemberSessionFilter : IActionFilter, IOrderedFilter
    {
        public int Order => int.MaxValue;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var items = context.HttpContext.Items;
            switch (context.HttpContext.Request.Headers["X-Test-InvalidMemberSession"].ToString())
            {
                case "member-missing": items.Remove("MemberId"); break;
                case "member-invalid": items["MemberId"] = "invalid"; break;
                case "name-missing": items.Remove("IdentifyName"); break;
                case "name-invalid": items["IdentifyName"] = Guid.NewGuid(); break;
                case "name-empty": items["IdentifyName"] = string.Empty; break;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
