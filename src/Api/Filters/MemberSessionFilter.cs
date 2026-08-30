using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Members.Commands.Provision;
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
public sealed class MemberSessionFilter : IAsyncActionFilter
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
    /// Executes the action filter asynchronously.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    /// <param name="next">The delegate to execute the next action filter or the action itself.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

        var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subjectId))
        {
            await next();
            return;
        }

        var preferredUsername = user.FindFirst("preferred_username")?.Value;
        string? identifyName;
        
        if (!string.IsNullOrEmpty(preferredUsername) && preferredUsername.Contains('@'))
        {
            var prefix = preferredUsername.Split('@')[0];
            var suffix = Guid.NewGuid().ToString()[..4];
            identifyName = $"{prefix}_{suffix}".ToLowerInvariant();
        }
        else if (!string.IsNullOrEmpty(preferredUsername))
        {
            identifyName = preferredUsername;
        }
        else
        {
            identifyName = $"{user.FindFirst(ClaimTypes.GivenName)?.Value}_{Guid.NewGuid().ToString()[..8]}".ToLowerInvariant();
        }

        var avatarUrlString = user.FindFirst("avatarUrl")?.Value;
        Uri? avatarUrl = null;
        if (!string.IsNullOrEmpty(avatarUrlString))
        {
            Uri.TryCreate(avatarUrlString, UriKind.Absolute, out avatarUrl);
        }

        var result = await _sender.Send(new ProvisionMemberCommand(
            SubjectId: subjectId,
            IdentifyName: identifyName,
            FirstName: user.FindFirst(ClaimTypes.GivenName)?.Value ?? "New",
            LastName: user.FindFirst(ClaimTypes.Surname)?.Value ?? "User",
            AvatarUrl: avatarUrl
        ));

        context.HttpContext.Items["MemberId"] = result.MemberId;
        context.HttpContext.Items["IdentifyName"] = result.IdentifyName;

        await next();
    }
}
