using Application.Shared;
using Quartz;
using Domain.Members;
using Infrastructure.BackgroundJobClient;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
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
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddPlanthorDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<PlanthorDbContext>(options =>
        {
            options.UseMongoDB(connectionString, "planthordb");
        });

        // Register your specific aggregate repositories (Manual DI)
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IBackgroundJobClient, QuartzBackgroundJobClient>();
        services.AddScoped<IAvatarStorageService, AzureBlobAvatarStorageService>();

        services.AddHttpClient();

        // Register Quartz.NET
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("DownloadAvatar");
            q.AddJob<DownloadAvatarJob>(opts => opts.WithIdentity(jobKey).StoreDurably());
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
