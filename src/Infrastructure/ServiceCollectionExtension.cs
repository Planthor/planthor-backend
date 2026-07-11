using System;
using Application.Shared;
using Quartz;
using Domain.Members;
using Domain.Plans;
using Infrastructure.BackgroundJobClient;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to add Planthor DbContext.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Adds and configures the Planthor DbContext.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddPlanthorDbContext(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<PlanthorDbContext>(options =>
        {
            options.UseMongoDB(connectionString, "planthordb");
        });

        // Register your specific aggregate repositories (Manual DI)
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IReadOnlyContext, ReadOnlyContext>();
        services.AddScoped<IBackgroundJobClient, QuartzBackgroundJobClient>();

        AddAvatarStorage(services, configuration);

        services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>();

        // Register Quartz.NET
        services.AddQuartz(q =>
        {
            var downloadJobKey = new JobKey("DownloadAvatar");
            q.AddJob<DownloadAvatarJob>(opts => opts.WithIdentity(downloadJobKey).StoreDurably());
            
            var syncIdentityJobKey = new JobKey("SyncIdentity");
            q.AddJob<Infrastructure.BackgroundJobClient.Jobs.SyncIdentityJob>(opts => opts.WithIdentity(syncIdentityJobKey).StoreDurably());
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }

    /// <summary>
    /// Registers the avatar storage implementation selected by <c>Storage:Provider</c>.
    /// One provider per environment — no need to support multiple simultaneously.
    /// </summary>
    private static void AddAvatarStorage(IServiceCollection services, IConfiguration configuration)
    {
        var providerValue = configuration["Storage:Provider"];
        if (!Enum.TryParse<StorageProviderType>(providerValue, ignoreCase: true, out var provider))
        {
            throw new InvalidOperationException(
                $"Storage:Provider '{providerValue}' is not a valid value. Expected: {string.Join(", ", Enum.GetNames<StorageProviderType>())}.");
        }
        switch (provider)
        {
            case StorageProviderType.Google:
                services.AddScoped<IAvatarStorageService, GoogleCloudAvatarStorageService>();
                break;
            case StorageProviderType.Azure:
                services.AddScoped<IAvatarStorageService, AzureBlobAvatarStorageService>();
                break;
            default:
                throw new InvalidOperationException($"No IAvatarStorageService registered for provider '{provider}'.");
        }
    }
}
