namespace Zogreo.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IEnumerable<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "OK") =>
        new() { Success = true, StatusCode = 200, Message = message, Data = data };

    public static ApiResponse<T> Created(T data, string message = "Created") =>
        new() { Success = true, StatusCode = 201, Message = message, Data = data };

    public static ApiResponse<T> Fail(int statusCode, string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors };
}

// Non-generic convenience for void responses
public static class ApiResponse
{
    public static ApiResponse<object?> Ok(string message = "OK") =>
        new() { Success = true, StatusCode = 200, Message = message, Data = null };

    public static ApiResponse<object?> Fail(int statusCode, string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors };
}
