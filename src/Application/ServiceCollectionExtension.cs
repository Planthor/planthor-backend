using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register application-layer services,
/// including MediatR handlers and FluentValidation validators.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Registers application-level dependencies, such as CQRS handlers and validators, 
    /// into the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to add dependencies to.</param>
    /// <param name="configuration">The application configuration properties.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddMediatR(cfg =>
        {
            var licenseKey = configuration["MediatR:LicenseKey"] ?? string.Empty;
            cfg.LicenseKey = licenseKey;

            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtension).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtension).Assembly);

        return services;
    }
}
