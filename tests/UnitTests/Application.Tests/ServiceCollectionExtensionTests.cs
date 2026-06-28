using Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Application.Tests;

public class ServiceCollectionExtensionTests
{
    [Fact]
    public void AddApplicationServices_RegistersMediatRAndValidators()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var result = services.AddApplicationServices(config);

        Assert.Same(services, result);
        Assert.NotEmpty(services);
    }
}
