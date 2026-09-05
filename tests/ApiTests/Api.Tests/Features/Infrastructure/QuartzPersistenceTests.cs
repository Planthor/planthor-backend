using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Npgsql;
using Quartz;
using Xunit;

namespace Api.Tests.Features.Infrastructure;

/// <summary>Verifies that API startup and background scheduling write to the real Quartz database.</summary>
/// <param name="factory">The API host with isolated MongoDB and PostgreSQL containers.</param>
public sealed class QuartzPersistenceTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    /// <summary>Checks the running API's persistent store and the durable jobs registered at startup.</summary>
    [Fact]
    public async Task Startup_WithQuartzConnectionString_PersistsRegisteredJobs()
    {
        // Arrange
        using var client = factory.CreateClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = timeout.Token;

        // Act
        using var response = await client.GetAsync("/v1/healthz", cancellationToken);
        var scheduler = await factory.Services.GetRequiredService<ISchedulerFactory>()
            .GetScheduler(cancellationToken);
        var metadata = await scheduler.GetMetaData(cancellationToken);
        await using var connection = new NpgsqlConnection(factory.Services
            .GetRequiredService<IConfiguration>().GetConnectionString("Quartz"));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT job_name FROM qrtz_job_details WHERE sched_name = @scheduler AND is_durable", connection);
        command.Parameters.AddWithValue("scheduler", scheduler.SchedulerName);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        List<string> jobNames = [];
        while (await reader.ReadAsync(cancellationToken))
        {
            jobNames.Add(reader.GetString(0));
        }

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(metadata.JobStoreSupportsPersistence);
        Assert.Contains("DownloadAvatar", jobNames);
        Assert.Contains("SyncIdentity", jobNames);
    }

    /// <summary>Checks that repeated delayed requests reuse a durable job and coalesce duplicate triggers.</summary>
    [Fact]
    public async Task EnqueueExternalActivitySyncAsync_WithRepeatedDelayedRequests_PersistsDistinctTriggersOnly()
    {
        // Arrange
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = timeout.Token;
        await using var scope = factory.Services.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var scheduler = await scope.ServiceProvider.GetRequiredService<ISchedulerFactory>()
            .GetScheduler(cancellationToken);
        var request = new ExternalActivitySyncJobRequest(
            "STRAVA", $"athlete-{Guid.NewGuid():N}", "manual", $"request-{Guid.NewGuid():N}",
            NotBefore: Instant.FromDateTimeOffset(TimeProvider.System.GetUtcNow().AddHours(1)));
        await using var connection = new NpgsqlConnection(scope.ServiceProvider
            .GetRequiredService<IConfiguration>().GetConnectionString("Quartz"));
        await connection.OpenAsync(cancellationToken);

        // Act
        await jobs.EnqueueExternalActivitySyncAsync(request, cancellationToken);
        await jobs.EnqueueExternalActivitySyncAsync(request, cancellationToken);
        await jobs.EnqueueExternalActivitySyncAsync(
            request with { IdempotencyKey = $"request-{Guid.NewGuid():N}" }, cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM qrtz_triggers AS trigger
            JOIN qrtz_job_details AS job USING (sched_name, job_name, job_group)
            WHERE trigger.sched_name = @scheduler
              AND trigger.job_group = 'external-activity-sync'
              AND trigger.trigger_state = 'WAITING'
              AND job.is_durable
            """, connection);
        command.Parameters.AddWithValue("scheduler", scheduler.SchedulerName);
        var pendingCount = await command.ExecuteScalarAsync(cancellationToken);

        // Assert
        Assert.Equal(2L, pendingCount);
    }

    /// <summary>Checks that repeated revocation requests remain idempotent with PostgreSQL persistence.</summary>
    [Fact]
    public async Task EnqueueExternalConnectionRevocationAsync_WithRepeatedRequest_PersistsSingleTrigger()
    {
        // Arrange
        using var isolatedFactory = factory.WithWebHostBuilder(_ => { });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = timeout.Token;
        await using var scope = isolatedFactory.Services.CreateAsyncScope();
        var scheduler = await scope.ServiceProvider.GetRequiredService<ISchedulerFactory>()
            .GetScheduler(cancellationToken);
        await scheduler.Standby(cancellationToken);
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var externalUserId = $"athlete-{Guid.NewGuid():N}";
        var idempotencyKey = $"revocation-{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(scope.ServiceProvider
            .GetRequiredService<IConfiguration>().GetConnectionString("Quartz"));
        await connection.OpenAsync(cancellationToken);

        // Act
        await jobs.EnqueueExternalConnectionRevocationAsync("STRAVA", externalUserId, idempotencyKey, cancellationToken);
        await jobs.EnqueueExternalConnectionRevocationAsync("STRAVA", externalUserId, idempotencyKey, cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM qrtz_triggers AS trigger
            JOIN qrtz_job_details AS job USING (sched_name, job_name, job_group)
            WHERE trigger.sched_name = @scheduler
              AND trigger.job_group = 'external-connection-revocation'
              AND job.is_durable
            """, connection);
        command.Parameters.AddWithValue("scheduler", scheduler.SchedulerName);
        var pendingCount = await command.ExecuteScalarAsync(cancellationToken);

        // Assert
        Assert.Equal(1L, pendingCount);
    }
}
