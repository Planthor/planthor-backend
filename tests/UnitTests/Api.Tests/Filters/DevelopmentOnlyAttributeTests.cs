using System;
using System.Collections.Generic;
using Api.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Xunit;

namespace Api.Tests.Filters;

public class DevelopmentOnlyAttributeTests
{
    [Fact]
    public void OnAuthorization_WhenEnvironmentIsDevelopment_DoesNothing()
    {
        // Arrange
        var envMock = Substitute.For<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(IWebHostEnvironment)).Returns(envMock);

        var httpContextMock = Substitute.For<HttpContext>();
        httpContextMock.Setup(hc => hc.RequestServices).Returns(serviceProviderMock);

        var actionContext = new ActionContext(
            httpContextMock,
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
        var envMock = Substitute.For<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(environment);

        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(IWebHostEnvironment)).Returns(envMock);

        var httpContextMock = Substitute.For<HttpContext>();
        httpContextMock.Setup(hc => hc.RequestServices).Returns(serviceProviderMock);

        var actionContext = new ActionContext(
            httpContextMock,
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
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        // Simulate missing IWebHostEnvironment
        serviceProviderMock.GetService(typeof(IWebHostEnvironment)).Returns(null!);

        var httpContextMock = Substitute.For<HttpContext>();
        httpContextMock.Setup(hc => hc.RequestServices).Returns(serviceProviderMock);

        var actionContext = new ActionContext(
            httpContextMock,
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
