using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Api.Tests.TestAuthentication;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;
using Xunit;

namespace Api.Tests;

/// <summary>
/// A factory for creating instances of the web application for integration testing.
/// This factory customizes the application's services for testing purposes.
/// </summary>
/// <typeparam name="TProgram">The type of the entry point class for the application.</typeparam>
public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly IContainer _mongoDbContainer = new ContainerBuilder()
        .WithImage("mongo:8.3")
        .WithCommand("mongod", "--replSet", "rs0", "--bind_ip_all")
        .WithPortBinding(27017, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
        .Build();
    public WireMockServer WireMockServer { get; private set; }

    public CustomWebApplicationFactory()
    {
        WireMockServer = WireMockServer.Start();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Strava:BaseUrl", WireMockServer.Url },
                { "Keycloak:BaseUrl", WireMockServer.Url }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the production database context with a test container one
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
                var connectionString = $"mongodb://{_mongoDbContainer.Hostname}:{_mongoDbContainer.GetMappedPublicPort(27017)}/?replicaSet=rs0&directConnection=true";
                options.UseMongoDB(connectionString, "planthordb_test");
            });

            services
            .AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                    options.DefaultForbidScheme = "TestScheme";
                })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                "TestScheme",
                options => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _mongoDbContainer.StartAsync();
        await Task.Delay(2000); // Give Mongo time to start listening
        await _mongoDbContainer.ExecAsync(["mongosh", "--quiet", "--eval", "rs.initiate()"]);
        await Task.Delay(2000); // Give Replica Set time to elect primary
    }
    public new async Task DisposeAsync()
    {
        WireMockServer.Stop();
        await _mongoDbContainer.DisposeAsync();
    }
}
