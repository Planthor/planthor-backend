using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.Tests.Features.Infrastructure;

/// <summary>Verifies that invalid scheduler configuration prevents the API from starting.</summary>
public sealed class QuartzStartupTests
{
    /// <summary>Rejects absent, empty, and whitespace connection strings with actionable configuration guidance.</summary>
    /// <param name="connectionString">The invalid Quartz connection string supplied to the API.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void Startup_WithMissingQuartzConnectionString_ThrowsConfigurationException(string? connectionString)
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Quartz"] = connectionString
                })));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        // Assert
        Assert.Contains("ConnectionStrings:Quartz is required", exception.Message);
        Assert.Contains("ConnectionStrings__Quartz", exception.Message);
    }
}
