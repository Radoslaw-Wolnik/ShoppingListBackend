using System.Runtime.CompilerServices;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Data;
using ShoppingListBackend.Api.Endpoints;
using ShoppingListBackend.Api.Extensions;
using ShoppingListBackend.Api.Hubs;
using ShoppingListBackend.Api.Middleware;
using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Api.Services;
[assembly: InternalsVisibleTo("ShoppingListBackend.Tests")]

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "ClientApp";

// -------------------------------
// 1. Database (PostgreSQL)
// -------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// -------------------------------
// 2. Repositories & Services
// -------------------------------
builder.Services.AddRepositories();
builder.Services.AddServices();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// -------------------------------
// 3. Authentication (API Key)
// -------------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "ApiKey";
    options.DefaultChallengeScheme = "ApiKey";
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(IsLoopbackOrigin)
                  .AllowCredentials();
        }
    });
});

// -------------------------------
// 4. Validation, SignalR, Health Checks
// -------------------------------
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
var signalRBuilder = builder.Services.AddSignalR();
var signalRRedisConnection = builder.Configuration.GetConnectionString("SignalRRedis");
if (!string.IsNullOrWhiteSpace(signalRRedisConnection))
{
    signalRBuilder.AddStackExchangeRedis(signalRRedisConnection);
}
builder.Services.AddHealthChecks();

// -------------------------------
// 5. Build App
// -------------------------------
var app = builder.Build();

// Local development applies migrations automatically; production deployments should run them explicitly.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
        dbContext.Database.Migrate();
    else
        dbContext.Database.EnsureCreated();
}

// -------------------------------
// 6. Middleware pipeline
// -------------------------------
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// -------------------------------
// 7. Endpoints
// -------------------------------
app.MapDeviceEndpoints();
app.MapShoppingListEndpoints();

// -------------------------------
// 8. SignalR Hub
// -------------------------------
app.MapHub<ShoppingListHub>("/hub/shoppingLists");

app.Run();

static bool IsLoopbackOrigin(string origin)
{
    return Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback;
}

public partial class Program { }
