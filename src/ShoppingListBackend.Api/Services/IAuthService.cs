using ShoppingListBackend.Api.DTOs;
using ShoppingListBackend.Api.Models;

namespace ShoppingListBackend.Api.Services;

public interface IAuthService
{
    Task<RegisterDeviceResponse> RegisterDeviceAsync();  // no parameter
    Task<Device?> ValidateApiKeyAsync(string apiKey);
}
