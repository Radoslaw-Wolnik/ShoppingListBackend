using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Data;
using ShoppingListBackend.Api.Endpoints;
using ShoppingListBackend.Api.Extensions;
using ShoppingListBackend.Api.Hubs;
using ShoppingListBackend.Api.Middleware;
using ShoppingListBackend.Api.Repositories.Implementations;
using ShoppingListBackend.Api.Repositories.Interfaces;
using ShoppingListBackend.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// 1. Database
// -------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// -------------------------------
// 2. Repositories & Services (using extensions)
// -------------------------------
builder.Services.AddRepositories();      // registers IDeviceRepository, IShoppingListRepository
builder.Services.AddServices();          // registers IAuthService, IHashService, IShoppingListService, IDeviceService

// Register the read repository (if not already included in AddRepositories)
builder.Services.AddScoped<IShoppingListReadRepository, ShoppingListReadRepository>();

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

// Ensure database is created (for development)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// -------------------------------
// 6. Middleware pipeline
// -------------------------------
app.UseAuthentication();   // must be before Authorization
app.UseAuthorization();

app.MapHealthChecks("/health");

// -------------------------------
// 7. Map Minimal API endpoints (using extension methods)
// -------------------------------
app.MapDeviceEndpoints();
app.MapShoppingListEndpoints();

// -------------------------------
// 8. Map SignalR hub
// -------------------------------
app.MapHub<ShoppingListHub>("/hub/shoppingLists");

app.Run();