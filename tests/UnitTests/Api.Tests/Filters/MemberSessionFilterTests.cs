using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Application.Members.Commands.Provision;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace Api.Tests.Filters;

public class MemberSessionFilterTests
{
    private readonly ISender _senderMock;
    private readonly MemberSessionFilter _filter;

    public MemberSessionFilterTests()
    {
        _senderMock = Substitute.For<ISender>();
        _senderMock.Send(Arg.Any<ProvisionMemberCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProvisionMemberResult(Guid.NewGuid(), "test_user")));
        _filter = new MemberSessionFilter(_senderMock);
    }

    private static ActionExecutingContext CreateContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: null!);
    }

    [Fact]
    public async Task OnActionExecutionAsync_UnauthenticatedUser_DoesNothing()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(user);
        var nextCalled = false;
        Task<ActionExecutedContext> next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        }

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(nextCalled);
        _ = await _senderMock.DidNotReceiveWithAnyArgs().Send(default!);
    }

    [Fact]
    public async Task OnActionExecutionAsync_MissingSubjectId_DoesNothing()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthType"); // IsAuthenticated = true
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user);
        var nextCalled = false;
        Task<ActionExecutedContext> next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        }

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(nextCalled);
        _ = await _senderMock.DidNotReceiveWithAnyArgs().Send(default!);
    }

    [Fact]
    public async Task OnActionExecutionAsync_EmailPreferredUsername_ExtractsPrefixAndAppendsSuffix()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthType");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "sub123"));
        identity.AddClaim(new Claim("preferred_username", "user@example.com"));
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user);

        Task<ActionExecutedContext> next() => Task.FromResult(new ActionExecutedContext(context, [], null!));

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        _ = _senderMock.Received(1).Send(Arg.Is<ProvisionMemberCommand>(c =>
            c.SubjectId == "sub123" &&
            c.IdentifyName.StartsWith("user_") &&
            c.IdentifyName.Length == 9 && // 'user_' (5) + 4 random chars = 9.
            c.FirstName == "New" && // default
            c.LastName == "User" && // default
            c.AvatarUrl == null
        ), Arg.Any<CancellationToken>());
        Assert.True(context.HttpContext.Items.ContainsKey("MemberId"));
        Assert.True(context.HttpContext.Items.ContainsKey("IdentifyName"));
        Assert.Equal("test_user", context.HttpContext.Items["IdentifyName"]);
    }

    [Fact]
    public async Task OnActionExecutionAsync_NonEmailPreferredUsername_UsesUsernameAsIs()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthType");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "sub123"));
        identity.AddClaim(new Claim("preferred_username", "my_custom_username"));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, "John"));
        identity.AddClaim(new Claim(ClaimTypes.Surname, "Doe"));
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user);

        Task<ActionExecutedContext> next() => Task.FromResult(new ActionExecutedContext(context, [], null!));

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        _ = _senderMock.Received(1).Send(Arg.Is<ProvisionMemberCommand>(c =>
            c.IdentifyName == "my_custom_username" &&
            c.FirstName == "John" &&
            c.LastName == "Doe"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoPreferredUsername_FallsBackToGivenName()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthType");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "sub123"));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, "Alice"));
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user);

        Task<ActionExecutedContext> next() => Task.FromResult(new ActionExecutedContext(context, [], null!));

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        _ = _senderMock.Received(1).Send(Arg.Is<ProvisionMemberCommand>(c =>
            c.IdentifyName.StartsWith("alice_") &&
            c.IdentifyName.Length == 14 // 'alice_' (6) + 8 random chars = 14
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoPreferredUsernameNoGivenName_FallsBackToGuid()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthType");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "sub123"));
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user);

        Task<ActionExecutedContext> next() => Task.FromResult(new ActionExecutedContext(context, [], null!));

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        _ = _senderMock.Received(1).Send(Arg.Is<ProvisionMemberCommand>(c =>
            c.IdentifyName.StartsWith('_') &&
            c.IdentifyName.Length == 9 // '_' (1) + 8 random chars = 9
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithValidAvatarUrl_SetsAvatarUrl()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthType");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "sub123"));
        identity.AddClaim(new Claim("preferred_username", "user1"));
        identity.AddClaim(new Claim("avatarUrl", "https://example.com/avatar.png"));
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user);

        Task<ActionExecutedContext> next() => Task.FromResult(new ActionExecutedContext(context, [], null!));

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        _ = _senderMock.Received(1).Send(Arg.Is<ProvisionMemberCommand>(c =>
            c.AvatarUrl != null && c.AvatarUrl.ToString() == "https://example.com/avatar.png"
        ), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task OnActionExecutionAsync_WithInvalidAvatarUrl_DoesNotThrowAndSetsNull()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthType");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "sub123"));
        identity.AddClaim(new Claim("preferred_username", "user1"));
        identity.AddClaim(new Claim("avatarUrl", "not_a_valid_url"));
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user);

        Task<ActionExecutedContext> next() => Task.FromResult(new ActionExecutedContext(context, [], null!));

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        _ = _senderMock.Received(1).Send(Arg.Is<ProvisionMemberCommand>(c =>
            c.AvatarUrl == null
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_NullSender_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new MemberSessionFilter(null!));
    }

    [Fact]
    public async Task OnActionExecutionAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        Task<ActionExecutedContext> next() => Task.FromResult<ActionExecutedContext>(null!);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => _filter.OnActionExecutionAsync(null!, next));
    }

    [Fact]
    public async Task OnActionExecutionAsync_NullNext_ThrowsArgumentNullException()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(user);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => _filter.OnActionExecutionAsync(context, null!));
    }
}
