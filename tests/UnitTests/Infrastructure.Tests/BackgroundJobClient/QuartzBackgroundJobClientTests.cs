using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Infrastructure.BackgroundJobClient;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using Quartz;

namespace Infrastructure.Tests.BackgroundJobClient;

public sealed class QuartzBackgroundJobClientTests
{
    private readonly ISchedulerFactory _schedulerFactory = Substitute.For<ISchedulerFactory>();
    private readonly IScheduler _scheduler = Substitute.For<IScheduler>();
    private readonly QuartzBackgroundJobClient _client;

    public QuartzBackgroundJobClientTests()
    {
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(_scheduler);
        _client = new QuartzBackgroundJobClient(
            _schedulerFactory,
            NullLogger<QuartzBackgroundJobClient>.Instance);
    }

    [Fact]
    public void Constructor_WithNullSchedulerFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<QuartzBackgroundJobClient>.Instance;

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzBackgroundJobClient(null!, logger));

        // Assert
        Assert.Equal("schedulerFactory", exception.ParamName);
    }

    [Fact]
    public async Task EnqueueAvatarDownloadAsync_WithValidRequest_TriggersRegisteredJob()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var avatarUrl = new Uri("https://images.planthor.test/avatar.png");

        // Act
        await _client.EnqueueAvatarDownloadAsync(memberId, avatarUrl, CancellationToken.None);

        // Assert
        await _scheduler.Received(1).TriggerJob(
            Arg.Is<JobKey>(key => key != null && key.Name == "DownloadAvatar"),
            Arg.Is<JobDataMap>(data =>
                data != null &&
                data.GetString("MemberId") == memberId.ToString() &&
                data.GetString("Url") == avatarUrl.ToString()),
            CancellationToken.None);
    }

    [Fact]
    public async Task EnqueueIdentitySyncAsync_WithValidRequest_TriggersRegisteredJob()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        // Act
        await _client.EnqueueIdentitySyncAsync(memberId, "subject-1", CancellationToken.None);

        // Assert
        await _scheduler.Received(1).TriggerJob(
            Arg.Is<JobKey>(key => key != null && key.Name == "SyncIdentity"),
            Arg.Is<JobDataMap>(data =>
                data != null &&
                data.GetString("MemberId") == memberId.ToString() &&
                data.GetString("IdentifyName") == "subject-1"),
            CancellationToken.None);
    }

    [Fact]
    public async Task EnqueueExternalActivitySyncAsync_WithImmediateAndDelayedRequests_BuildsDurableJobAndTriggers()
    {
        // Arrange
        var notBefore = Instant.FromUtc(2026, 9, 3, 10, 15);
        var immediate = new ExternalActivitySyncJobRequest(
            " strava ",
            "athlete-1",
            "webhook",
            "event-immediate",
            "activity-1");
        var delayed = immediate with
        {
            Trigger = "retry",
            IdempotencyKey = "event-delayed",
            ExternalActivityId = null,
            NotBefore = notBefore,
            RetryCount = 2
        };

        // Act
        await _client.EnqueueExternalActivitySyncAsync(immediate, CancellationToken.None);
        await _client.EnqueueExternalActivitySyncAsync(delayed, CancellationToken.None);

        // Assert
        await _scheduler.Received(2).AddJob(
            Arg.Is<IJobDetail>(job =>
                job != null &&
                job.Key.Group == "external-activity-sync" &&
                job.Key.Name.StartsWith("STRAVA-", StringComparison.Ordinal) &&
                job.Durable &&
                job.RequestsRecovery &&
                job.JobDataMap.GetString("ProviderId") == " strava " &&
                job.JobDataMap.GetString("ExternalUserId") == "athlete-1"),
            false,
            false,
            CancellationToken.None);
        await _scheduler.Received(1).ScheduleJob(
            Arg.Is<ITrigger>(trigger =>
                trigger != null &&
                trigger.Key.Group == "external-activity-sync" &&
                trigger.JobDataMap.GetString("Trigger") == "webhook" &&
                trigger.JobDataMap.GetString("ExternalActivityId") == "activity-1" &&
                trigger.JobDataMap.GetString("RetryCount") == "0" &&
                trigger.StartTimeUtc <= DateTimeOffset.UtcNow),
            CancellationToken.None);
        await _scheduler.Received(1).ScheduleJob(
            Arg.Is<ITrigger>(trigger =>
                trigger != null &&
                trigger.Key.Group == "external-activity-sync" &&
                trigger.JobDataMap.GetString("Trigger") == "retry" &&
                trigger.JobDataMap.GetString("ExternalActivityId") == "" &&
                trigger.JobDataMap.GetString("RetryCount") == "2" &&
                trigger.StartTimeUtc == notBefore.ToDateTimeOffset()),
            CancellationToken.None);
    }

    [Fact]
    public async Task EnqueueExternalActivitySyncAsync_WhenJobAndTriggerExist_CoalescesDuplicates()
    {
        // Arrange
        _scheduler.AddJob(
                Arg.Any<IJobDetail>(),
                false,
                false,
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new ObjectAlreadyExistsException("Job exists."));
        _scheduler.ScheduleJob(
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>())
            .Returns<DateTimeOffset>(_ => throw new ObjectAlreadyExistsException("Trigger exists."));
        var request = new ExternalActivitySyncJobRequest(
            "STRAVA",
            "athlete-1",
            "webhook",
            "event-1");

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _client.EnqueueExternalActivitySyncAsync(request, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task EnqueueExternalConnectionRevocationAsync_WithValidRequest_BuildsDurableJobAndTrigger()
    {
        // Arrange
        const string providerId = " strava ";
        const string externalUserId = "athlete-1";

        // Act
        await _client.EnqueueExternalConnectionRevocationAsync(
            providerId,
            externalUserId,
            "event-1",
            CancellationToken.None);

        // Assert
        await _scheduler.Received(1).AddJob(
            Arg.Is<IJobDetail>(job =>
                job != null &&
                job.Key.Group == "external-connection-revocation" &&
                job.Key.Name.StartsWith("STRAVA-", StringComparison.Ordinal) &&
                job.Durable &&
                job.RequestsRecovery &&
                job.JobDataMap.GetString("ProviderId") == providerId &&
                job.JobDataMap.GetString("ExternalUserId") == externalUserId),
            false,
            false,
            CancellationToken.None);
        await _scheduler.Received(1).ScheduleJob(
            Arg.Is<ITrigger>(trigger =>
                trigger != null &&
                trigger.Key.Group == "external-connection-revocation" &&
                trigger.JobKey.Group == "external-connection-revocation"),
            CancellationToken.None);
    }

    [Fact]
    public async Task EnqueueExternalConnectionRevocationAsync_WhenJobAndTriggerExist_CoalescesDuplicates()
    {
        // Arrange
        _scheduler.AddJob(
                Arg.Any<IJobDetail>(),
                false,
                false,
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new ObjectAlreadyExistsException("Job exists."));
        _scheduler.ScheduleJob(
                Arg.Any<ITrigger>(),
                Arg.Any<CancellationToken>())
            .Returns<DateTimeOffset>(_ => throw new ObjectAlreadyExistsException("Trigger exists."));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _client.EnqueueExternalConnectionRevocationAsync(
                "STRAVA",
                "athlete-1",
                "event-1",
                CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task CancelExternalActivitySyncAsync_WithValidRequest_DeletesNormalizedJob()
    {
        // Arrange
        const string providerId = " strava ";

        // Act
        await _client.CancelExternalActivitySyncAsync(
            providerId,
            "athlete-1",
            CancellationToken.None);

        // Assert
        await _scheduler.Received(1).DeleteJob(
            Arg.Is<JobKey>(key =>
                key != null &&
                key.Group == "external-activity-sync" &&
                key.Name.StartsWith("STRAVA-", StringComparison.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task PublicMethods_WithMissingRequiredArguments_ThrowArgumentExceptions()
    {
        // Arrange
        var validId = Guid.NewGuid();

        // Act
        var avatarException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _client.EnqueueAvatarDownloadAsync(validId, null!, CancellationToken.None));
        var identityException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _client.EnqueueIdentitySyncAsync(validId, "", CancellationToken.None));
        var activityException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _client.EnqueueExternalActivitySyncAsync(null!, CancellationToken.None));
        var providerException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _client.EnqueueExternalConnectionRevocationAsync("", "athlete", "event", CancellationToken.None));
        var userException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _client.EnqueueExternalConnectionRevocationAsync("STRAVA", "", "event", CancellationToken.None));
        var idempotencyException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _client.EnqueueExternalConnectionRevocationAsync("STRAVA", "athlete", "", CancellationToken.None));
        var cancelProviderException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _client.CancelExternalActivitySyncAsync("", "athlete", CancellationToken.None));
        var cancelUserException = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _client.CancelExternalActivitySyncAsync("STRAVA", "", CancellationToken.None));

        // Assert
        Assert.Equal("avatarUrl", avatarException.ParamName);
        Assert.NotNull(identityException);
        Assert.Equal("request", activityException.ParamName);
        Assert.NotNull(providerException);
        Assert.NotNull(userException);
        Assert.NotNull(idempotencyException);
        Assert.NotNull(cancelProviderException);
        Assert.NotNull(cancelUserException);
    }
}
