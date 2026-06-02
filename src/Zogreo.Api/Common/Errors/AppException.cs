namespace Zogreo.Api.Common.Errors;

public class AppException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static AppException NotFound(string msg = "Not found") => new(msg, 404);
    public static AppException Forbidden(string msg = "Forbidden") => new(msg, 403);
    public static AppException Conflict(string msg = "Conflict") => new(msg, 409);
    public static AppException Unauthorized(string msg = "Unauthorized") => new(msg, 401);
}
