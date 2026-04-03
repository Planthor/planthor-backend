using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Tests.TestAuthentication;

public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static readonly string TestUserRolesHeader = "X-TestUserRoles";
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rolesHeader = Request.Headers[TestUserRolesHeader].FirstOrDefault() ?? "User";

        var roles = rolesHeader.Split(",", StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim> {
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };

        // Add role claims after trimming whitespace
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
