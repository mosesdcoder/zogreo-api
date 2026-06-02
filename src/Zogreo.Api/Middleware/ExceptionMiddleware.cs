using System.Text.Json;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Domain.Exceptions;

namespace Zogreo.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions _json =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (ValidationException ex)
        {
            await WriteErrorAsync(ctx, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (AppException ex)
        {
            await WriteErrorAsync(ctx, ex.StatusCode, ex.Message);
        }
        catch (InvalidStateTransitionException ex)
        {
            await WriteErrorAsync(ctx, 409, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(ctx, 500, "An unexpected error occurred.");
        }
    }

    private static Task WriteErrorAsync(
        HttpContext ctx, int statusCode, string message,
        IEnumerable<string>? errors = null)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";

        var envelope = new
        {
            success = false,
            statusCode,
            message,
            data = (object?)null,
            errors
        };

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(envelope, _json));
    }
}
