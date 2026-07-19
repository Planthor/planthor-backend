using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using NSubstitute;

namespace Infrastructure.Tests.Context;

public class PlanthorDbContextTests
{
    [Fact]
    public void Model_ShouldBuildSuccessfully_WithoutConstructorBindingErrors()
    {
        // Arrange
        var clientMock = Substitute.For<IMongoClient>();
        
        var options = new DbContextOptionsBuilder<PlanthorDbContext>()
            .UseMongoDB(clientMock, "DummyDb")
            .Options;
            
        var publisherMock = Substitute.For<IPublisher>();
        
        using var context = new PlanthorDbContext(options, publisherMock);
        
        // Act
        // Accessing the Model property forces EF Core to build and validate the model.
        // If any mapped entity or complex type is missing a suitable constructor,
        // this will throw an InvalidOperationException.
        var exception = Record.Exception(() => _ = context.Model);
        
        // Assert
        Assert.Null(exception);
    }
}
