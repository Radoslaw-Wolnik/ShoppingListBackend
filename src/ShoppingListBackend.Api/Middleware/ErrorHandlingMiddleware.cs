using System.Net;
using System.Text.Json;
using FluentValidation;
using ShoppingListBackend.Api.Exceptions;

namespace ShoppingListBackend.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private static readonly HashSet<Type> _handledExceptions = new()
    {
        typeof(ValidationException),
        typeof(KeyNotFoundException),
        typeof(UnauthorizedAccessException),
        typeof(InvalidOperationException)
    };

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Error processing request: {Path}", context.Request.Path);
        context.Response.ContentType = "application/problem+json";

        // Get problem details from the exception or a default mapping
        var (statusCode, title, detail, extensions) = GetProblemDetails(exception);

        context.Response.StatusCode = statusCode;

        var problem = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path,
            traceId = context.TraceIdentifier,
            extensions = extensions
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        await context.Response.WriteAsync(json);
    }

    private (int statusCode, string title, string detail, object? extensions) GetProblemDetails(Exception ex)
    {
        // Custom exceptions that define their own details
        if (ex is AppException appEx)
        {
            return (appEx.StatusCode, appEx.Title, appEx.Message, appEx.Extensions);
        }

        // FluentValidation
        if (ex is ValidationException validationEx)
        {
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return (400, "Validation Failed", "One or more validation errors occurred.", new { errors });
        }

        // Built‑in exceptions mapping
        return ex switch
        {
            KeyNotFoundException => (404, "Not Found", ex.Message, null),
            UnauthorizedAccessException => (403, "Forbidden", ex.Message, null),
            InvalidOperationException => (400, "Bad Request", ex.Message, null),
            _ => (500, "Internal Server Error", "An unexpected error occurred.", null)
        };
    }
}