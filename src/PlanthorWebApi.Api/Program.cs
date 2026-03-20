using System;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using NSwag.Generation.Processors.Security;
using PlanthorWebApi.Domain.Shared;
using PlanthorWebApi.Infrastructure;
using PlanthorWebApi.Infrastructure.Authentication;
using PlanthorWebApi.Infrastructure.Repositories;
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

    builder.Services.AddScoped(typeof(IWriteRepository<>), typeof(BaseWriteRepository<>));
    builder.Services.AddScoped(typeof(IReadRepository<>), typeof(BaseReadRepository<>));

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
    builder.Services.AddOpenApiDocument(options =>
    {
        options.PostProcess = document =>
        {
            document.Info = new OpenApiInfo
            {
                Version = "v0.0.1",
                Title = "Planthor Web API",
                Description = "A robust and scalable .NET Web API playing as a main resource server for Planthor",
                Contact = new OpenApiContact
                {
                    Name = "Trung Pham",
                    Url = "https://github.com/zovippro1996"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = "https://github.com/Planthor/PlanthorWebApi?tab=MIT-1-ov-file#readme"
                }
            };
        };

        // SwaggerUI JWTAuthentication
        options.AddSecurity(
            "JWTBearerAuthentication",
            new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            }
        );
        options.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWTBearerAuthentication"));
    });

    builder.Services.AddMediatR(cfg =>
    {
        var mediatRAssemblies = Array.Empty<object>();
        cfg.RegisterServicesFromAssemblies((System.Reflection.Assembly[])mediatRAssemblies);
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        // Add OpenAPI 3.0 document serving middleware
        // Available at: http://localhost:<port>/swagger/v1/swagger.json
        app.UseOpenApi();

        // Add web UIs to interact with the document
        // Available at: http://localhost:<port>/swagger
        app.UseSwaggerUi();
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
