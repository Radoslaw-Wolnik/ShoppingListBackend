using ShoppingListBackend.Api.Repositories.Interfaces;
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
        services.AddSingleton<IHashService, BcryptHashService>(); // or scoped
        return services;
    }
}