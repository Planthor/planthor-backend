using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Lightweight implementation of <see cref="IKeycloakAdminClient"/> using HttpClient.
/// </summary>
public partial class KeycloakAdminClient(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakAdminClient> logger) : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<KeycloakAdminClient> _logger = logger;

    /// <inheritdoc />
    public Task<List<FederatedIdentityDto>> GetUserFederatedIdentitiesAsync(string identifyName)
        => GetUserFederatedIdentitiesAsync(identifyName, CancellationToken.None);

    /// <inheritdoc />
    public async Task<List<FederatedIdentityDto>> GetUserFederatedIdentitiesAsync(string identifyName, CancellationToken cancellationToken)
    {
        var authority = _configuration["Authentication:Keycloak:Authority"];
        var clientId = _configuration["Authentication:Keycloak:ClientId"] ?? "planthor-backend";
        var clientSecret = _configuration["Authentication:Keycloak:ClientSecret"];

        if (string.IsNullOrEmpty(authority))
        {
            throw new InvalidOperationException("Keycloak Authority is not configured.");
        }

        // Get Client Credentials Token
        var tokenEndpoint = $"{authority}/protocol/openid-connect/token";
        
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret ?? string.Empty }
            })
        };

        var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        using var tokenDocument = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString();

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Failed to retrieve access token from Keycloak.");
        }

        // Extract base URL from authority (e.g. https://auth.planthor.space/realms/planthor)
        // Admin API is at https://auth.planthor.space/admin/realms/planthor/...
        var uri = new Uri(authority);
        var baseUrl = $"{uri.Scheme}://{uri.Authority}";
        var realm = uri.Segments[^1].TrimEnd('/'); // Gets "planthor"
        
        var adminEndpoint = $"{baseUrl}/admin/realms/{realm}/users/{identifyName}/federated-identity";

        var request = new HttpRequestMessage(HttpMethod.Get, adminEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            LogFetchFailed(identifyName, response.StatusCode);
            return new List<FederatedIdentityDto>();
        }

        var result = await response.Content.ReadFromJsonAsync<List<FederatedIdentityDto>>(cancellationToken: cancellationToken);
        return result ?? [];
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch federated identities for user {IdentifyName}. Status: {StatusCode}")]
    private partial void LogFetchFailed(string identifyName, System.Net.HttpStatusCode statusCode);
}
