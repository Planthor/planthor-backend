using Adapters.Strava.Client;
using Adapters.Strava.Configuration;
using Adapters.Strava.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adapters.Strava;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register
/// Strava adapter services, including the API client, token database, and configuration.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Adds and configures all Strava adapter services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration containing the <c>Strava</c> section.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddStravaAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<StravaOptions>(configuration.GetSection(StravaOptions.SectionName));

        // Token storage (singleton — MongoClient is thread-safe)
        services.AddSingleton<StravaAdapterDatabase>();

        // Typed HTTP client for Strava API
        services.AddHttpClient<IStravaApiClient, StravaApiClient>();

        return services;
    }
}
