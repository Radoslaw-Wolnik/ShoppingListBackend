using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Api.Services;

namespace ShoppingListBackend.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IShoppingListService, ShoppingListService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IShoppingListReadService, ShoppingListReadService>();
        services.AddScoped<IEditingTracker, DatabaseEditingTracker>();
        services.AddSingleton<IHashService, BcryptHashService>();
        return services;
    }
}
