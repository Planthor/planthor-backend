using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Members.Commands.Provision;
using Application.Members.Queries.CheckExists;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

/// <summary>
/// An action filter that ensures a member session exists for the authenticated user.
/// If the user is authenticated but does not exist in the Planthor system,
/// this filter triggers a Just-In-Time (JIT) provisioning process.
/// </summary>
/// <remarks>
/// This filter delegates actual authentication checks to the default [Authorize] filter.
/// It only acts if the user is already authenticated by the identity provider (e.g., Keycloak).
/// </remarks>
public class MemberSessionFilter : IAsyncActionFilter
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberSessionFilter"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender used to dispatch provisioning commands.</param>
    public MemberSessionFilter(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return OnActionExecutionInternalAsync(context, next);
    }

    private async Task OnActionExecutionInternalAsync(ActionContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var identifyName = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identifyName))
        {
            await next();
            return;
        }

        var exists = await _sender.Send(new CheckMemberExistsQuery(identifyName));
        if (exists)
        {
            await next();
            return;
        }

        var avatarUrlString = user.FindFirst("avatarUrl")?.Value;
        Uri? avatarUrl = null;
        if (!string.IsNullOrEmpty(avatarUrlString))
        {
            Uri.TryCreate(avatarUrlString, UriKind.Absolute, out avatarUrl);
        }

        await _sender.Send(new ProvisionMemberCommand(
            IdentifyName: identifyName,
            FirstName: user.FindFirst(ClaimTypes.GivenName)?.Value ?? "New",
            LastName: user.FindFirst(ClaimTypes.Surname)?.Value ?? "User",
            AvatarUrl: avatarUrl
        ));

        await next();
    }
}
