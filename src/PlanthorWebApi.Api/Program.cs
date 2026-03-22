using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlanthorWebApi.Infrastructure;
using PlanthorWebApi.Infrastructure.Authentication;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting web application");
    var builder = WebApplication.CreateBuilder(args);

    // Infrastructure
    builder.Host.UseSerilog();

    builder.Services.AddPlanthorDbContext(
        builder.Configuration.GetConnectionString("PlanthorDbContext")
            ?? throw new InvalidOperationException("PlanthorDbContext is not set in the configuration file."));

    builder.Services.AddScoped<IUserService, UserService>();
    builder
        .Services
        // .AddAuthentication("BasicAuthentication")
        // .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null)
        .AddAuthentication()
        .AddJwtBearer(options =>
        {
            options.Authority = "https://localhost:5001";
            options.Audience = "planthorAPI";
        });

    // API Client
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // OpenAPI + Scalar
    builder.Services.AddOpenApi();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        // Serves the OpenAPI JSON document at /openapi/v1.json
        app.MapOpenApi();

        // Serves the Scalar UI at /scalar/v1
        app.MapScalarApiReference(options =>
        {
            options.Title = "Planthor API";
            options.Theme = ScalarTheme.DeepSpace;
            options.DefaultHttpClient = new(ScalarTarget.Swift, ScalarClient.HttpClient);
        });
    }

    app.MapControllers();

    Log.Information("The app started.");
    app.Run();
}
catch (InvalidOperationException ex)  // Catch specific exception
{
    // Log detailed exception information for InvalidOperationException
    Log.Error(ex, "An unexpected operation occurred.");
}
catch (AppDomainUnloadedException ex)
{
    Log.Fatal(ex, "The application domain was unloaded unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Make Program extensible for integration tests
/// </summary>
public partial class Program
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class.
    /// </summary>
    protected Program() { }
}
