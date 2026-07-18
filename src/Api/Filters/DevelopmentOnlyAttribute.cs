using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Filters;

/// <summary>
/// An authorization filter that restricts access to an endpoint so that it is only available in the Development environment.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DevelopmentOnlyAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// Checks if the application is running in the Development environment.
    /// Returns 404 Not Found otherwise.
    /// </summary>
    /// <param name="context">The authorization filter context.</param>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var env = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();

        if (env == null || !env.IsDevelopment())
        {
            context.Result = new NotFoundResult();
        }
    }
}
