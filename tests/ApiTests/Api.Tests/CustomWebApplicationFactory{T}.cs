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
    private readonly IContainer _mongoDbContainer = new ContainerBuilder("mongo:8.3")
        .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
        .WithCommand("mongod", "--replSet", "rs0", "--bind_ip_all")
        .WithPortBinding(27017, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017))
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

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Strava:BaseUrl", WireMockServer.Url },
                { "Strava:ClientId", "test-client-id" },
                { "Strava:ClientSecret", "test-client-secret" },
                { "Strava:RedirectUri", "https://api.planthor.test/v1/Strava/callback" },
                { "Strava:StateEncryptionKey", "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=" },
                { "Strava:Scopes", "activity:read_all,profile:read_all" },
                { "Strava:FrontendSuccessUrl", "https://app.planthor.test/connections?status=success" },
                { "Strava:FrontendErrorUrl", "https://app.planthor.test/connections?status=error" },
                { "Strava:WebhookVerifyToken", "test-webhook-token" },
                { "Strava:WebhookSubscriptionId", "99" },
                { "Strava:AutomaticSyncEnabled", "false" },
                { "ConnectionStrings:Quartz", "" },
                { "Keycloak:BaseUrl", WireMockServer.Url },
                { "Authentication:Keycloak:Authority", $"{WireMockServer.Url}/realms/planthor" },
                { "Authentication:Keycloak:ClientId", "test-client" },
                { "Authentication:Keycloak:ClientSecret", "test-secret" }
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

            var mongoClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(MongoDB.Driver.IMongoClient));

            if (mongoClientDescriptor != null)
            {
                services.Remove(mongoClientDescriptor);
            }

            var connectionString = $"mongodb://{_mongoDbContainer.Hostname}:{_mongoDbContainer.GetMappedPublicPort(27017)}/?replicaSet=rs0&directConnection=true";
            var mongoClient = new MongoDB.Driver.MongoClient(connectionString);
            services.AddSingleton<MongoDB.Driver.IMongoClient>(mongoClient);

            services.AddDbContext<PlanthorDbContext>(options =>
            {
                options.UseMongoDB(mongoClient, "planthordb_test");
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
