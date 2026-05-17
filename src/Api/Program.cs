using System;
using Api.Filters;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
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

    builder.Services.AddSingleton<IClock>(SystemClock.Instance);

    builder.Services.AddPlanthorDbContext(
        builder.Configuration.GetConnectionString("PlanthorDbContext")
            ?? throw new InvalidOperationException("PlanthorDbContext is not set in the configuration file."));

    builder.Services.AddApplicationServices(builder.Configuration);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Authentication:Keycloak:Authority"] ?? "http://localhost:8180/realms/planthor";
            options.Audience = builder.Configuration["Authentication:Keycloak:Audience"] ?? "planthor-backend";
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Authentication:Keycloak:Authority"] ?? "http://localhost:8180/realms/planthor",
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Authentication:Keycloak:Audience"] ?? "planthor-backend",
                ValidateLifetime = true,
            };
        });

    builder.Services.AddAuthorization();

    // API Client
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<MemberSessionFilter>();
    });

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

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("The app started.");
    await app.RunAsync();
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
