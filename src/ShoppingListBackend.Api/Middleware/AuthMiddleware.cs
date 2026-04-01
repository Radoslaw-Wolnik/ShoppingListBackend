using System.Security.Claims;
using ShoppingListBackend.Api.Services;

namespace ShoppingListBackend.Api.Middleware;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeader = "X-API-Key";

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        // Skip authentication for registration endpoint
        if (context.Request.Path.StartsWithSegments("/api/register"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing");
            return;
        }

        var device = await authService.ValidateApiKeyAsync(apiKey!);
        if (device == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        // Create a claims identity with the device ID
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, device.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}