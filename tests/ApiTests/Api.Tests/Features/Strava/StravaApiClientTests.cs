using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Adapters.Strava.Client;
using Adapters.Strava.Persistence;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace Api.Tests.Features.Strava;

public class StravaApiClientTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly IStravaApiClient _apiClient;
    private readonly StravaAdapterDatabase _tokenDb;

    public StravaApiClientTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        var scope = _factory.Services.CreateScope();
        _apiClient = scope.ServiceProvider.GetRequiredService<IStravaApiClient>();
        _tokenDb = scope.ServiceProvider.GetRequiredService<StravaAdapterDatabase>();
    }

    [Fact]
    public async Task ExchangeCodeAsync_Success()
    {
        var memberId = Guid.NewGuid().ToString("N");
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"access_token\":\"acc_123\",\"refresh_token\":\"ref_123\",\"expires_at\":1234567890,\"athlete\":{\"id\":12345}}"));

        var response = await _apiClient.ExchangeCodeAsync("dummy_code", memberId, CancellationToken.None);
        
        Assert.NotNull(response);
        Assert.Equal("acc_123", response?.AccessToken);
        
        // Refresh Token
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("grant_type=refresh_token")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"access_token\":\"acc_new\",\"refresh_token\":\"ref_new\",\"expires_at\":1234567890}"));

        var refresh = await _apiClient.RefreshTokenAsync(memberId, CancellationToken.None);
        Assert.NotNull(refresh);
        Assert.Equal("acc_new", refresh?.AccessToken);
        
        // GetValidToken - Should get from DB without refresh if not expired
        // But the previous refresh set expires_at = 1234567890 which is in the past! 
        // So GetValidToken will attempt refresh!
        // Let's setup the token endpoint again for refresh:
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("grant_type=refresh_token")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"access_token\":\"acc_fresh\",\"refresh_token\":\"ref_fresh\",\"expires_at\":2147483647}"));

        var valid = await _apiClient.GetValidTokenAsync(memberId, CancellationToken.None);
        Assert.NotNull(valid);
        Assert.Equal("acc_fresh", valid?.AccessToken);

        // Call again - this time it shouldn't expire
        var validAgain = await _apiClient.GetValidTokenAsync(memberId, CancellationToken.None);
        Assert.NotNull(validAgain);
        Assert.Equal("acc_fresh", validAgain?.AccessToken);

        // Deauthorize
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/deauthorize").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        var deauth = await _apiClient.DeauthorizeAsync(memberId, CancellationToken.None);
        Assert.True(deauth);
        
        var afterDeauth = await _apiClient.GetValidTokenAsync(memberId, CancellationToken.None);
        Assert.Null(afterDeauth);
    }
    
    [Fact]
    public async Task ApiClient_Error_Branches()
    {
        var memberId = Guid.NewGuid().ToString("N");
        
        // Exchange error
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("error_test")))
            .RespondWith(Response.Create().WithStatusCode(400));

        var exchFail = await _apiClient.ExchangeCodeAsync("error_test", memberId, CancellationToken.None);
        Assert.Null(exchFail);

        // Refresh missing token
        var refreshFail = await _apiClient.RefreshTokenAsync(Guid.NewGuid().ToString("N"), CancellationToken.None);
        Assert.Null(refreshFail);
        
        // GetValidToken missing token
        var validFail = await _apiClient.GetValidTokenAsync(Guid.NewGuid().ToString("N"), CancellationToken.None);
        Assert.Null(validFail);

        // Deauth missing token
        var deauthFail = await _apiClient.DeauthorizeAsync(Guid.NewGuid().ToString("N"), CancellationToken.None);
        Assert.True(deauthFail);
    }

    [Fact]
    public async Task ApiClient_Error_And_Null_Responses()
    {
        var memberId = Guid.NewGuid().ToString("N");
        
        // Exchange returns literal null (deserializes to null)
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("null_exch")))
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("null"));

        var exchNull = await _apiClient.ExchangeCodeAsync("null_exch", memberId, CancellationToken.None);
        Assert.Null(exchNull);

        // Seed token for refresh and deauth tests
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("seed_exch")))
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"access_token\":\"acc\",\"refresh_token\":\"ref_err\",\"expires_at\":0,\"athlete\":{\"id\":1}}"));
        await _apiClient.ExchangeCodeAsync("seed_exch", memberId, CancellationToken.None);

        // Refresh Token Error (400)
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("refresh_token=ref_err")))
            .RespondWith(Response.Create().WithStatusCode(400));
        var refreshError = await _apiClient.RefreshTokenAsync(memberId, CancellationToken.None);
        Assert.Null(refreshError);

        // Update token to use ref_null
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("seed_null")))
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"access_token\":\"acc\",\"refresh_token\":\"ref_null\",\"expires_at\":0,\"athlete\":{\"id\":1}}"));
        await _apiClient.ExchangeCodeAsync("seed_null", memberId, CancellationToken.None);

        // Refresh Token returns literal null
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("refresh_token=ref_null")))
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("null"));
        var refreshNull = await _apiClient.RefreshTokenAsync(memberId, CancellationToken.None);
        Assert.Null(refreshNull);

        // Deauthorize error
        // Deauth uses access_token, so let's match the token we got from seed_null
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/deauthorize").UsingPost().WithBody(b => b != null && b.Contains("access_token=acc")))
            .RespondWith(Response.Create().WithStatusCode(400));
        var deauthError = await _apiClient.DeauthorizeAsync(memberId, CancellationToken.None);
        Assert.True(deauthError);
    }

    [Fact]
    public async Task GetAthleteActivitiesAsync_Tests()
    {
        var memberId = Guid.NewGuid().ToString("N");
        
        // No token
        var actsEmpty = await _apiClient.GetAthleteActivitiesPageAsync(memberId, 0, 100, 1, 30, CancellationToken.None);
        Assert.Equal(StravaApiOutcome.AuthorizationRequired, actsEmpty.Outcome);

        // Seed valid token
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("valid_exch")))
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"access_token\":\"acc_val\",\"refresh_token\":\"ref_val\",\"expires_at\":2147483647,\"athlete\":{\"id\":1}}"));
        await _apiClient.ExchangeCodeAsync("valid_exch", memberId, CancellationToken.None);

        // API returns activities
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/api/v3/athlete/activities").UsingGet().WithHeader("Authorization", "Bearer acc_val"))
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("[{\"id\":101,\"name\":\"Morning Run\"}]"));
        
        var acts = await _apiClient.GetAthleteActivitiesPageAsync(memberId, 12345678, 22345678, 1, 30, CancellationToken.None);
        Assert.Equal(StravaApiOutcome.Success, acts.Outcome);
        Assert.Single(acts.Value!);

        // API returns 400
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/api/v3/athlete/activities").UsingGet().WithHeader("Authorization", "Bearer acc_fail"))
            .RespondWith(Response.Create().WithStatusCode(400));
            
        // Seed new token to hit 400
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/oauth/token").UsingPost().WithBody(b => b != null && b.Contains("fail_exch")))
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"access_token\":\"acc_fail\",\"refresh_token\":\"ref_fail\",\"expires_at\":2147483647,\"athlete\":{\"id\":2}}"));
        await _apiClient.ExchangeCodeAsync("fail_exch", memberId, CancellationToken.None);

        var actsFail = await _apiClient.GetAthleteActivitiesPageAsync(memberId, 0, 100, 1, 30, CancellationToken.None);
        Assert.Equal(StravaApiOutcome.TransientFailure, actsFail.Outcome);
    }

    [Fact]
    public async Task GetActivityAsync_WithoutToken_ReturnsAuthorizationRequired()
    {
        // Arrange
        var memberId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _apiClient.GetActivityAsync(memberId, "123", CancellationToken.None);

        // Assert
        Assert.Equal(StravaApiOutcome.AuthorizationRequired, result.Outcome);
        Assert.Equal("strava_token_missing", result.ErrorCode);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task GetActivityAsync_WithMatchingAndMismatchingOwner_ReturnsExpectedOutcome(
        bool includeAthlete,
        bool ownerMatches)
    {
        // Arrange
        var memberId = Guid.NewGuid().ToString("N");
        var activityId = Random.Shared.NextInt64(100_000, 999_999).ToString();
        var accessToken = $"owner-{Guid.NewGuid():N}";
        await SeedTokenAsync(memberId, accessToken, 42);
        var athleteJson = includeAthlete
            ? $",\"athlete\":{{\"id\":{(ownerMatches ? 42 : 99)}}}"
            : "";
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/api/v3/activities/{activityId}")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {accessToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"id\":{activityId},\"name\":\"Run\"{athleteJson}}}"));

        // Act
        var result = await _apiClient.GetActivityAsync(memberId, activityId, CancellationToken.None);

        // Assert
        Assert.Equal(
            ownerMatches ? StravaApiOutcome.Success : StravaApiOutcome.AuthorizationRequired,
            result.Outcome);
        Assert.Equal(ownerMatches ? null : "strava_activity_owner_mismatch", result.ErrorCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "NOT_FOUND", "strava_activity_not_found")]
    [InlineData(HttpStatusCode.Forbidden, "AUTHORIZATION_REQUIRED", "strava_authorization_required")]
    [InlineData(HttpStatusCode.TooManyRequests, "RATE_LIMITED", "strava_rate_limited")]
    [InlineData(HttpStatusCode.BadGateway, "TRANSIENT_FAILURE", "strava_temporarily_unavailable")]
    public async Task GetActivityAsync_WithProviderFailure_MapsTypedOutcome(
        HttpStatusCode statusCode,
        string expectedOutcomeId,
        string expectedErrorCode)
    {
        // Arrange
        var expectedOutcome = StravaApiOutcome.FromId(expectedOutcomeId);
        var memberId = Guid.NewGuid().ToString("N");
        var activityId = Random.Shared.NextInt64(100_000, 999_999).ToString();
        var accessToken = $"failure-{Guid.NewGuid():N}";
        await SeedTokenAsync(memberId, accessToken, 42);
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/api/v3/activities/{activityId}")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {accessToken}"))
            .RespondWith(Response.Create().WithStatusCode(statusCode));

        // Act
        var result = await _apiClient.GetActivityAsync(memberId, activityId, CancellationToken.None);

        // Assert
        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        var expectsRetry = expectedOutcome == StravaApiOutcome.RateLimited ||
            expectedOutcome == StravaApiOutcome.TransientFailure;
        Assert.Equal(expectsRetry, result.RetryAt is not null);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{ invalid json")]
    public async Task GetActivityAsync_WithInvalidSuccessPayload_ReturnsTransientFailure(string responseBody)
    {
        // Arrange
        var memberId = Guid.NewGuid().ToString("N");
        var activityId = Random.Shared.NextInt64(100_000, 999_999).ToString();
        var accessToken = $"payload-{Guid.NewGuid():N}";
        await SeedTokenAsync(memberId, accessToken, 42);
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/api/v3/activities/{activityId}")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {accessToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseBody));

        // Act
        var result = await _apiClient.GetActivityAsync(memberId, activityId, CancellationToken.None);

        // Assert
        Assert.Equal(StravaApiOutcome.TransientFailure, result.Outcome);
        Assert.Equal("strava_temporarily_unavailable", result.ErrorCode);
        Assert.NotNull(result.RetryAt);
    }

    [Fact]
    public async Task GetActivityAsync_WhenUnauthorizedAndRefreshFails_ReturnsAuthorizationRequired()
    {
        // Arrange
        var memberId = Guid.NewGuid().ToString("N");
        var activityId = Random.Shared.NextInt64(100_000, 999_999).ToString();
        var accessToken = $"refresh-failure-{Guid.NewGuid():N}";
        var refreshToken = $"refresh-{Guid.NewGuid():N}";
        await SeedTokenAsync(memberId, accessToken, 42, refreshToken);
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/api/v3/activities/{activityId}")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {accessToken}"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath("/oauth/token")
                .UsingPost()
                .WithBody(body => body != null && body.Contains($"refresh_token={refreshToken}")))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.BadGateway));

        // Act
        var result = await _apiClient.GetActivityAsync(memberId, activityId, CancellationToken.None);

        // Assert
        Assert.Equal(StravaApiOutcome.AuthorizationRequired, result.Outcome);
        Assert.Equal("strava_token_refresh_failed", result.ErrorCode);
    }

    [Fact]
    public async Task GetActivityAsync_WhenUnauthorizedAndRefreshSucceeds_RetriesWithRotatedToken()
    {
        // Arrange
        var memberId = Guid.NewGuid().ToString("N");
        var activityId = Random.Shared.NextInt64(100_000, 999_999).ToString();
        var accessToken = $"old-{Guid.NewGuid():N}";
        var refreshedAccessToken = $"new-{Guid.NewGuid():N}";
        var refreshToken = $"refresh-{Guid.NewGuid():N}";
        await SeedTokenAsync(memberId, accessToken, 42, refreshToken);
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/api/v3/activities/{activityId}")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {accessToken}"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath("/oauth/token")
                .UsingPost()
                .WithBody(body => body != null && body.Contains($"refresh_token={refreshToken}")))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"access_token\":\"{refreshedAccessToken}\",\"refresh_token\":\"rotated\",\"expires_at\":2147483647}}"));
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/api/v3/activities/{activityId}")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {refreshedAccessToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"id\":{activityId},\"name\":\"Retried run\",\"athlete\":{{\"id\":42}}}}"));

        // Act
        var result = await _apiClient.GetActivityAsync(memberId, activityId, CancellationToken.None);

        // Assert
        Assert.Equal(StravaApiOutcome.Success, result.Outcome);
        Assert.Equal(long.Parse(activityId), result.Value?.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetActivityAsync_WithMissingArguments_ThrowsArgumentException(string? missingValue)
    {
        // Arrange
        var validValue = Guid.NewGuid().ToString("N");

        // Act
        var missingMemberException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _apiClient.GetActivityAsync(missingValue!, validValue, CancellationToken.None));
        var missingActivityException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _apiClient.GetActivityAsync(validValue, missingValue!, CancellationToken.None));

        // Assert
        Assert.NotNull(missingMemberException);
        Assert.NotNull(missingActivityException);
    }

    private async Task SeedTokenAsync(
        string memberId,
        string accessToken,
        long athleteId,
        string? refreshToken = null)
    {
        var authorizationCode = $"seed-{Guid.NewGuid():N}";
        var persistedRefreshToken = refreshToken ?? $"refresh-{Guid.NewGuid():N}";
        _factory.WireMockServer
            .Given(Request.Create()
                .WithPath("/oauth/token")
                .UsingPost()
                .WithBody(body => body != null && body.Contains($"code={authorizationCode}")))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(
                    $"{{\"access_token\":\"{accessToken}\",\"refresh_token\":\"{persistedRefreshToken}\",\"expires_at\":2147483647,\"athlete\":{{\"id\":{athleteId}}}}}"));

        var token = await _apiClient.ExchangeCodeAsync(
            authorizationCode,
            memberId,
            CancellationToken.None);

        Assert.NotNull(token);
    }
}
