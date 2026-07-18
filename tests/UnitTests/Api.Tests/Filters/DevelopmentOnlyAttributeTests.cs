using System;
using System.Collections.Generic;
using Api.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Api.Tests.Filters;

public class DevelopmentOnlyAttributeTests
{
    [Fact]
    public void OnAuthorization_WhenEnvironmentIsDevelopment_DoesNothing()
    {
        // Arrange
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWebHostEnvironment))).Returns(envMock.Object);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(hc => hc.RequestServices).Returns(serviceProviderMock.Object);

        var actionContext = new ActionContext(
            httpContextMock.Object,
            new RouteData(),
            new ActionDescriptor()
        );

        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        var attribute = new DevelopmentOnlyAttribute();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Test")]
    public void OnAuthorization_WhenEnvironmentIsNotDevelopment_ReturnsNotFound(string environment)
    {
        // Arrange
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(environment);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWebHostEnvironment))).Returns(envMock.Object);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(hc => hc.RequestServices).Returns(serviceProviderMock.Object);

        var actionContext = new ActionContext(
            httpContextMock.Object,
            new RouteData(),
            new ActionDescriptor()
        );

        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        var attribute = new DevelopmentOnlyAttribute();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.IsType<NotFoundResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenEnvironmentIsNull_ReturnsNotFound()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        // Simulate missing IWebHostEnvironment
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWebHostEnvironment))).Returns(null!);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(hc => hc.RequestServices).Returns(serviceProviderMock.Object);

        var actionContext = new ActionContext(
            httpContextMock.Object,
            new RouteData(),
            new ActionDescriptor()
        );

        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        var attribute = new DevelopmentOnlyAttribute();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.IsType<NotFoundResult>(context.Result);
    }
}
