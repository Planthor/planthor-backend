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

public class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public static readonly string TestUserRolesHeader = "X-TestUserRoles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rolesHeader = Request.Headers[TestUserRolesHeader].FirstOrDefault() ?? "User";

        var roles = rolesHeader.Split(",", StringSplitOptions.RemoveEmptyEntries);

        if (Request.Headers.ContainsKey("X-Force-Unauthorized"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Forced unauthorized"));
        }
        var userIdHeader = Request.Headers["X-TestUserId"].FirstOrDefault() ?? "test-user-id-123";

        var claims = new List<Claim> {
            new(ClaimTypes.Name, "TestUser")
        };

        if (!Request.Headers.ContainsKey("X-Omit-NameIdentifier"))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdHeader));
        }

        AddOptionalClaim(claims, "X-TestPreferredUsername", "preferred_username");
        AddOptionalClaim(claims, "X-TestGivenName", ClaimTypes.GivenName);
        AddOptionalClaim(claims, "X-TestSurname", ClaimTypes.Surname);
        AddOptionalClaim(claims, "X-TestAvatarUrl", "avatarUrl");

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

    private void AddOptionalClaim(List<Claim> claims, string headerName, string claimType)
    {
        var value = Request.Headers[headerName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }
}
