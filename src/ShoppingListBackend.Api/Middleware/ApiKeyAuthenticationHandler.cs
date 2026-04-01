// Middleware/ApiKeyAuthenticationHandler.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingListBackend.Api.Services;

namespace ShoppingListBackend.Api.Middleware;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IAuthService _authService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IAuthService authService)
        : base(options, logger, encoder, clock)
    {
        _authService = authService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Try header first (HTTP requests)
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
        {
            // Try query string (SignalR WebSocket)
            if (!Request.Query.TryGetValue("apiKey", out apiKey))
            {
                return AuthenticateResult.NoResult();
            }
        }

        var key = apiKey.ToString();
        if (string.IsNullOrEmpty(key))
            return AuthenticateResult.NoResult();

        var device = await _authService.ValidateApiKeyAsync(key);
        if (device == null)
            return AuthenticateResult.Fail("Invalid API key");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, device.Id.ToString()),
            new Claim(ClaimTypes.Name, device.UserName ?? device.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}