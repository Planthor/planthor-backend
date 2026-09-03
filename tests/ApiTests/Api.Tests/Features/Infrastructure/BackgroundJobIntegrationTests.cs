using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.ExternalSync.Commands.ProcessExternalActivitySync;
using Application.Shared;
using Domain.Members;
using Infrastructure.BackgroundJobClient.Jobs;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using Quartz;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Api.Tests.Features.Infrastructure;

public sealed class BackgroundJobIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task DownloadAvatarJob_WithEmptyJobData_ReturnsWithoutDownloading()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var job = new DownloadAvatarJob(
            httpClientFactory,
            Substitute.For<IAvatarStorageService>(),
            Substitute.For<ISender>(),
            NullLogger<DownloadAvatarJob>.Instance);
        var emptyMemberContext = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = "",
            ["Url"] = ""
        });
        var emptyUrlContext = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = Guid.NewGuid().ToString(),
            ["Url"] = ""
        });

        // Act
        await job.Execute(emptyMemberContext);
        await job.Execute(emptyUrlContext);

        // Assert
        httpClientFactory.DidNotReceiveWithAnyArgs().CreateClient(default!);
    }

    [Fact]
    public async Task DownloadAvatarJob_WhenDownloadFails_ThrowsJobExecutionException()
    {
        // Arrange
        var path = $"/avatars/failure-{Guid.NewGuid():N}.jpg";
        factory.WireMockServer
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.BadGateway));
        await using var scope = factory.Services.CreateAsyncScope();
        var job = new DownloadAvatarJob(
            scope.ServiceProvider.GetRequiredService<IHttpClientFactory>(),
            Substitute.For<IAvatarStorageService>(),
            scope.ServiceProvider.GetRequiredService<ISender>(),
            NullLogger<DownloadAvatarJob>.Instance);
        var context = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = Guid.NewGuid().ToString(),
            ["Url"] = $"{factory.WireMockServer.Url}{path}"
        });

        // Act
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => job.Execute(context));

        // Assert
        Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.False(exception.RefireImmediately);
    }

    [Fact]
    public async Task DownloadAvatarJob_WithMissingContentType_UsesJpegAndUpdatesMember()
    {
        // Arrange
        var path = $"/avatars/no-content-type-{Guid.NewGuid():N}";
        factory.WireMockServer
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(new byte[] { 1, 2, 3, 4 }));
        var storage = new RecordingAvatarStorageService();
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var member = Member.Create(
            $"AVATAR_{Guid.NewGuid():N}",
            "Avatar",
            "",
            "Member",
            "",
            "UTC",
            NodaTime.SystemClock.Instance);
        await repository.AddAsync(member, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var job = new DownloadAvatarJob(
            scope.ServiceProvider.GetRequiredService<IHttpClientFactory>(),
            storage,
            scope.ServiceProvider.GetRequiredService<ISender>(),
            NullLogger<DownloadAvatarJob>.Instance);
        var context = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = member.Id.ToString(),
            ["Url"] = $"{factory.WireMockServer.Url}{path}"
        });

        // Act
        await job.Execute(context);

        // Assert
        var updatedMember = await repository.GetByIdAsync(member.Id, CancellationToken.None);
        Assert.Equal("image/jpeg", storage.ContentType);
        Assert.Equal(4, storage.BytesUploaded);
        Assert.Equal("https://cdn.planthor.test/default-avatar.jpg", updatedMember?.PathAvatar);
    }

    [Fact]
    public async Task DownloadAvatarJob_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var job = new DownloadAvatarJob(
            scope.ServiceProvider.GetRequiredService<IHttpClientFactory>(),
            Substitute.For<IAvatarStorageService>(),
            scope.ServiceProvider.GetRequiredService<ISender>(),
            NullLogger<DownloadAvatarJob>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => job.Execute(null!));

        // Assert
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public async Task SyncIdentityJob_WithInvalidMissingMemberAndUnlinkedMember_ReturnsWithoutProviderCall()
    {
        // Arrange
        var keycloakClient = Substitute.For<IKeycloakAdminClient>();
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var unlinkedMember = Member.Create(
            $"UNLINKED_{Guid.NewGuid():N}",
            "Unlinked",
            "",
            "Member",
            "",
            "UTC",
            NodaTime.SystemClock.Instance);
        await repository.AddAsync(unlinkedMember, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var job = new SyncIdentityJob(
            keycloakClient,
            repository,
            NodaTime.SystemClock.Instance,
            NullLogger<SyncIdentityJob>.Instance);
        var invalidIdContext = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = "not-a-guid",
            ["IdentifyName"] = "INVALID"
        });
        var emptyNameContext = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = Guid.NewGuid().ToString(),
            ["IdentifyName"] = ""
        });
        var missingMemberContext = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = Guid.NewGuid().ToString(),
            ["IdentifyName"] = "MISSING"
        });
        var unlinkedMemberContext = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = unlinkedMember.Id.ToString(),
            ["IdentifyName"] = unlinkedMember.IdentifyName
        });

        // Act
        await job.Execute(invalidIdContext);
        await job.Execute(emptyNameContext);
        await job.Execute(missingMemberContext);
        await job.Execute(unlinkedMemberContext);

        // Assert
        await keycloakClient.DidNotReceiveWithAnyArgs()
            .GetUserFederatedIdentitiesAsync(default!, default);
    }

    [Fact]
    public async Task SyncIdentityJob_WhenProviderClientFails_RethrowsFailure()
    {
        // Arrange
        var keycloakClient = Substitute.For<IKeycloakAdminClient>();
        keycloakClient
            .GetUserFederatedIdentitiesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<List<FederatedIdentityDto>>>(_ => throw new HttpRequestException("Keycloak unavailable."));
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var member = Member.Create(
            $"SYNC_FAILURE_{Guid.NewGuid():N}",
            "Sync",
            "",
            "Failure",
            "",
            "UTC",
            NodaTime.SystemClock.Instance);
        member.ConnectExternalProvider(
            ExternalProvider.Keycloak,
            ExternalConnectionType.Identity,
            $"subject-{Guid.NewGuid():N}",
            [],
            NodaTime.SystemClock.Instance);
        await repository.AddAsync(member, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var job = new SyncIdentityJob(
            keycloakClient,
            repository,
            NodaTime.SystemClock.Instance,
            NullLogger<SyncIdentityJob>.Instance);
        var context = CreateJobContext(new JobDataMap
        {
            ["MemberId"] = member.Id.ToString(),
            ["IdentifyName"] = member.IdentifyName
        });

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => job.Execute(context));

        // Assert
        Assert.Equal("Keycloak unavailable.", exception.Message);
    }

    [Fact]
    public async Task SyncIdentityJob_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var job = ActivatorUtilities.CreateInstance<SyncIdentityJob>(scope.ServiceProvider);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => job.Execute(null!));

        // Assert
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public async Task ProcessExternalActivitySyncJob_WithProviderDeferral_EnqueuesTypedRetry()
    {
        // Arrange
        var now = Instant.FromUtc(2026, 9, 3, 10, 0);
        var retryAt = now.Plus(Duration.FromMinutes(12));
        var sender = Substitute.For<ISender>();
        sender.Send(
                Arg.Any<ProcessExternalActivitySyncCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(new ProcessExternalActivitySyncResult(
                0,
                retryAt,
                "external_rate_limited"));
        var backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        var job = new ProcessExternalActivitySyncJob(
            sender,
            backgroundJobClient,
            new TestClock(now),
            NullLogger<ProcessExternalActivitySyncJob>.Instance);
        var context = CreateJobContext(new JobDataMap
        {
            ["ProviderId"] = "STRAVA",
            ["ExternalUserId"] = "42",
            ["Trigger"] = "webhook",
            ["IdempotencyKey"] = "event-1",
            ["ExternalActivityId"] = "activity-1",
            ["RetryCount"] = "2"
        });

        // Act
        await job.Execute(context);

        // Assert
        await backgroundJobClient.Received(1).EnqueueExternalActivitySyncAsync(
            Arg.Is<ExternalActivitySyncJobRequest>(request =>
                request != null &&
                request.ProviderId == "STRAVA" &&
                request.ExternalUserId == "42" &&
                request.Trigger == "webhook" &&
                request.ExternalActivityId == "activity-1" &&
                request.NotBefore == retryAt &&
                request.RetryCount == 2 &&
                request.IdempotencyKey == $"event-1:retry:{retryAt.ToUnixTimeSeconds()}:2"),
            CancellationToken.None);
    }

    [Fact]
    public async Task ProcessExternalActivitySyncJob_WithDefaultOptionalData_DispatchesRetryTriggerWithoutEnqueue()
    {
        // Arrange
        var sender = Substitute.For<ISender>();
        ProcessExternalActivitySyncCommand? dispatchedCommand = null;
        sender.Send(
                Arg.Do<ProcessExternalActivitySyncCommand>(command => dispatchedCommand = command),
                Arg.Any<CancellationToken>())
            .Returns(new ProcessExternalActivitySyncResult(0));
        var backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        var job = new ProcessExternalActivitySyncJob(
            sender,
            backgroundJobClient,
            new TestClock(Instant.FromUtc(2026, 9, 3, 10, 0)),
            NullLogger<ProcessExternalActivitySyncJob>.Instance);
        var context = CreateJobContext(new JobDataMap
        {
            ["ProviderId"] = "STRAVA",
            ["ExternalUserId"] = "42",
            ["Trigger"] = null!,
            ["IdempotencyKey"] = null!,
            ["ExternalActivityId"] = " ",
            ["RetryCount"] = "invalid"
        });

        // Act
        await job.Execute(context);

        // Assert
        Assert.NotNull(dispatchedCommand);
        Assert.Equal("retry", dispatchedCommand.Request.Trigger);
        Assert.Null(dispatchedCommand.Request.ExternalActivityId);
        Assert.Equal(0, dispatchedCommand.Request.RetryCount);
        Assert.Equal(32, dispatchedCommand.Request.IdempotencyKey.Length);
        await backgroundJobClient.DidNotReceiveWithAnyArgs()
            .EnqueueExternalActivitySyncAsync(default!, default);
    }

    [Theory]
    [InlineData("0", 1, 1)]
    [InlineData("1", 5, 2)]
    [InlineData("2", 15, 3)]
    public async Task ProcessExternalActivitySyncJob_WhenInfrastructureFails_SchedulesBoundedBackoff(
        string retryCount,
        int delayMinutes,
        int expectedRetryCount)
    {
        // Arrange
        var now = Instant.FromUtc(2026, 9, 3, 10, 0);
        var sender = Substitute.For<ISender>();
        sender.Send(
                Arg.Any<ProcessExternalActivitySyncCommand>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<ProcessExternalActivitySyncResult>>(_ =>
                throw new HttpRequestException("Temporary failure."));
        var backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        var job = new ProcessExternalActivitySyncJob(
            sender,
            backgroundJobClient,
            new TestClock(now),
            NullLogger<ProcessExternalActivitySyncJob>.Instance);
        var context = CreateJobContext(CreateActivitySyncData(retryCount));
        var expectedRetryAt = now.Plus(Duration.FromMinutes(delayMinutes));

        // Act
        await job.Execute(context);

        // Assert
        await backgroundJobClient.Received(1).EnqueueExternalActivitySyncAsync(
            Arg.Is<ExternalActivitySyncJobRequest>(request =>
                request != null &&
                request.NotBefore == expectedRetryAt &&
                request.RetryCount == expectedRetryCount &&
                request.IdempotencyKey ==
                    $"event-retry:retry:{expectedRetryAt.ToUnixTimeSeconds()}:{expectedRetryCount}"),
            CancellationToken.None);
    }

    [Fact]
    public async Task ProcessExternalActivitySyncJob_AfterMaximumRetries_RethrowsWithoutEnqueue()
    {
        // Arrange
        var sender = Substitute.For<ISender>();
        sender.Send(
                Arg.Any<ProcessExternalActivitySyncCommand>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<ProcessExternalActivitySyncResult>>(_ =>
                throw new HttpRequestException("Persistent failure."));
        var backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        var job = new ProcessExternalActivitySyncJob(
            sender,
            backgroundJobClient,
            new TestClock(Instant.FromUtc(2026, 9, 3, 10, 0)),
            NullLogger<ProcessExternalActivitySyncJob>.Instance);
        var context = CreateJobContext(CreateActivitySyncData("3"));

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => job.Execute(context));

        // Assert
        Assert.Equal("Persistent failure.", exception.Message);
        await backgroundJobClient.DidNotReceiveWithAnyArgs()
            .EnqueueExternalActivitySyncAsync(default!, default);
    }

    [Fact]
    public async Task ProcessExternalActivitySyncJob_WhenExecutionIsCancelled_RethrowsWithoutEnqueue()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var sender = Substitute.For<ISender>();
        sender.Send(
                Arg.Any<ProcessExternalActivitySyncCommand>(),
                cancellationSource.Token)
            .Returns<Task<ProcessExternalActivitySyncResult>>(_ =>
                throw new OperationCanceledException(cancellationSource.Token));
        var backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        var job = new ProcessExternalActivitySyncJob(
            sender,
            backgroundJobClient,
            new TestClock(Instant.FromUtc(2026, 9, 3, 10, 0)),
            NullLogger<ProcessExternalActivitySyncJob>.Instance);
        var context = CreateJobContext(CreateActivitySyncData("0"), cancellationSource.Token);

        // Act
        await Assert.ThrowsAsync<OperationCanceledException>(() => job.Execute(context));

        // Assert
        await backgroundJobClient.DidNotReceiveWithAnyArgs()
            .EnqueueExternalActivitySyncAsync(default!, default);
    }

    [Fact]
    public async Task ProcessExternalActivitySyncJob_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var job = new ProcessExternalActivitySyncJob(
            Substitute.For<ISender>(),
            Substitute.For<IBackgroundJobClient>(),
            NodaTime.SystemClock.Instance,
            NullLogger<ProcessExternalActivitySyncJob>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => job.Execute(null!));

        // Assert
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public async Task KeycloakAdminClient_WithNonSuccessAndNullResponses_ReturnsEmptyLists()
    {
        // Arrange
        var firstUser = $"failed-{Guid.NewGuid():N}";
        var secondUser = $"null-{Guid.NewGuid():N}";
        ConfigureKeycloakTokenResponse();
        factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/admin/realms/planthor/users/{firstUser}/federated-identity")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.BadGateway));
        factory.WireMockServer
            .Given(Request.Create()
                .WithPath($"/admin/realms/planthor/users/{secondUser}/federated-identity")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("null"));
        await using var scope = factory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IKeycloakAdminClient>();

        // Act
        var failedResult = await client.GetUserFederatedIdentitiesAsync(firstUser);
        var nullResult = await client.GetUserFederatedIdentitiesAsync(secondUser, CancellationToken.None);

        // Assert
        Assert.Empty(failedResult);
        Assert.Empty(nullResult);
    }

    [Fact]
    public async Task KeycloakAdminClient_WithEmptyAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        factory.WireMockServer
            .Given(Request.Create()
                .WithPath("/realms/planthor/protocol/openid-connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{ \"access_token\": \"\" }"));
        await using var scope = factory.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IKeycloakAdminClient>();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetUserFederatedIdentitiesAsync("empty-token", CancellationToken.None));

        // Assert
        Assert.Equal("Failed to retrieve access token from Keycloak.", exception.Message);
    }

    [Fact]
    public async Task KeycloakAdminClient_WithMissingAuthority_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var client = new KeycloakAdminClient(
            new HttpClient(),
            configuration,
            NullLogger<KeycloakAdminClient>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetUserFederatedIdentitiesAsync("member", CancellationToken.None));

        // Assert
        Assert.Equal("Keycloak Authority is not configured.", exception.Message);
    }

    [Fact]
    public async Task AzureBlobAvatarStorageService_UploadAndDelete_UsesAzureBlobHttpProtocol()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server
            .Given(Request.Create().UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Created)
                .WithHeader("ETag", "\"test-etag\"")
                .WithHeader("Last-Modified", "Thu, 03 Sep 2026 00:00:00 GMT")
                .WithHeader("x-ms-request-id", Guid.NewGuid().ToString())
                .WithHeader("x-ms-version", "2025-11-05")
                .WithHeader("x-ms-request-server-encrypted", "true"));
        server
            .Given(Request.Create().UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Accepted)
                .WithHeader("x-ms-request-id", Guid.NewGuid().ToString())
                .WithHeader("x-ms-version", "2025-11-05"));
        var accountKey = Convert.ToBase64String(new byte[32]);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Azure:ConnectionString"] =
                    $"DefaultEndpointsProtocol=http;AccountName=testaccount;" +
                    $"AccountKey={accountKey};BlobEndpoint={server.Url}/testaccount;"
            })
            .Build();
        var service = new AzureBlobAvatarStorageService(configuration);
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var memberId = Guid.NewGuid();

        // Act
        var avatarUriString = await service.UploadAvatarAsync(
            memberId,
            content,
            "image/png",
            CancellationToken.None);
        await service.DeleteAvatarAsync(new Uri(avatarUriString), CancellationToken.None);

        // Assert
        Assert.StartsWith(
            $"{server.Url}/testaccount/",
            avatarUriString,
            StringComparison.Ordinal);
        Assert.Contains(memberId.ToString(), avatarUriString, StringComparison.Ordinal);
        Assert.EndsWith(".png", avatarUriString, StringComparison.Ordinal);
        Assert.Contains(server.LogEntries, entry => entry.RequestMessage?.Method == "PUT");
        Assert.Contains(server.LogEntries, entry => entry.RequestMessage?.Method == "DELETE");
    }

    private void ConfigureKeycloakTokenResponse()
    {
        factory.WireMockServer
            .Given(Request.Create()
                .WithPath("/realms/planthor/protocol/openid-connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{ \"access_token\": \"keycloak-admin-token\" }"));
    }

    private static JobDataMap CreateActivitySyncData(string retryCount) =>
        new()
        {
            ["ProviderId"] = "STRAVA",
            ["ExternalUserId"] = "42",
            ["Trigger"] = "retry",
            ["IdempotencyKey"] = "event-retry",
            ["ExternalActivityId"] = "activity-1",
            ["RetryCount"] = retryCount
        };

    private static IJobExecutionContext CreateJobContext(
        JobDataMap dataMap,
        CancellationToken cancellationToken = default)
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(dataMap);
        context.CancellationToken.Returns(cancellationToken);
        return context;
    }

    private sealed class TestClock(Instant current) : IClock
    {
        public Instant GetCurrentInstant() => current;
    }

    private sealed class RecordingAvatarStorageService : IAvatarStorageService
    {
        public string? ContentType { get; private set; }

        public int BytesUploaded { get; private set; }

        public Task DeleteAvatarAsync(Uri blobUri, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public async Task<string> UploadAvatarAsync(
            Guid memberId,
            Stream fileStream,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await fileStream.CopyToAsync(buffer, cancellationToken);
            ContentType = contentType;
            BytesUploaded = (int)buffer.Length;
            return "https://cdn.planthor.test/default-avatar.jpg";
        }
    }
}
