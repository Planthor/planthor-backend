using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Tests.Services;

public class AzureBlobAvatarStorageServiceTests
{
    private static IConfiguration CreateConfig(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Azure:ConnectionString"] = connectionString
            })
            .Build();

    [Fact]
    public void Constructor_MissingConnectionString_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new AzureBlobAvatarStorageService(CreateConfig(null)));
    }

    [Fact]
    public async Task DeleteAvatarAsync_NullUri_ThrowsArgumentNullException()
    {
        var service = new AzureBlobAvatarStorageService(CreateConfig("DefaultEndpointsProtocol=https;AccountName=fake;AccountKey=ZmFrZWtleQ==;EndpointSuffix=core.windows.net"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.DeleteAvatarAsync(null!, CancellationToken.None));
    }
}
