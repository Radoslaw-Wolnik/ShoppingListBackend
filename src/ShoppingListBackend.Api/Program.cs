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

// Read repository & read service
builder.Services.AddScoped<IShoppingListReadService, ShoppingListReadService>();

// Real‑time presence tracker
builder.Services.AddSingleton<IEditingTracker, InMemoryEditingTracker>();

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

// -------------------------------
// 4. Validation, SignalR, Health Checks
// -------------------------------
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

// -------------------------------
// 5. Build App
// -------------------------------
var app = builder.Build();

// Ensure database is created (development only)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// -------------------------------
// 6. Middleware pipeline
// -------------------------------
app.UseMiddleware<ErrorHandlingMiddleware>();
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

public partial class Program { }