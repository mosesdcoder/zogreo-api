using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zogreo.Api.Filters;

/// <summary>
/// Automatically wraps every successful controller ObjectResult in the standard
/// { success, statusCode, message, data, errors } envelope.
/// ExceptionMiddleware handles the error path using the same shape.
/// </summary>
public class ApiResponseFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult objectResult) return;

        var statusCode = objectResult.StatusCode ?? 200;
        if (statusCode is < 200 or >= 300) return;

        // Skip if already in envelope format (marked by type)
        if (objectResult.Value is AlreadyWrapped) return;

        var message = statusCode == 201 ? "Created" : "OK";

        var envelope = new
        {
            success = true,
            statusCode,
            message,
            data = objectResult.Value,
            errors = (IEnumerable<string>?)null
        };

        context.Result = new ObjectResult(envelope) { StatusCode = statusCode };
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}

/// <summary>Marker — controllers can return this to bypass the filter.</summary>
public sealed record AlreadyWrapped(object? Data);
