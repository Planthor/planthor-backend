using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceCollectionExtension
{
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
