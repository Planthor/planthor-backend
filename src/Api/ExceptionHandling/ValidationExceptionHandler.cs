using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.ExceptionHandling;

internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        System.Exception exception,
        CancellationToken cancellationToken)
    {
        List<object>? errors = null;

        if (exception is ValidationException fluentEx)
        {
            errors = [];
            foreach (var failure in fluentEx.Errors)
            {
                errors.Add(new { field = failure.PropertyName, message = failure.ErrorMessage });
            }
        }
        else if (exception is DomainValidationException domainEx)
        {
            errors = [];
            foreach (var e in domainEx.Errors)
            {
                errors.Add(new { field = e.Field, message = e.Message, code = e.Code });
            }
        }

        if (errors is null)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        };
        problem.Extensions["errors"] = errors;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
