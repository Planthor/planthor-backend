using System;
using System.Data.Common;
using System.Linq;
using Api.Tests.TestAuthentication;
using Infrastructure.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

/// <summary>
/// A factory for creating instances of the web application for integration testing.
/// This factory customizes the application's services for testing purposes.
/// </summary>
/// <typeparam name="TProgram">The type of the entry point class for the application.</typeparam>
public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            // Replace the production database context with an in-memory one
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PlanthorDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            services.AddDbContext<PlanthorDbContext>(options =>
            {
                options.UseInMemoryDatabase("PlanthorInMemoryDb");
                options.ConfigureWarnings(warning => warning.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services
            .AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestSCheme";
                })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                "TestScheme",
                options => { });
        });
    }
}
