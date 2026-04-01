using ShoppingListBackend.Api.DTOs;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Repositories.Interfaces;
using ShoppingListBackend.Api.Services;

namespace ShoppingListBackend.Api.Services;

public class AuthService(IDeviceRepository deviceRepo, IHashService hashService, AppDbContext context) : IAuthService
{
    private readonly IDeviceRepository _deviceRepo = deviceRepo;
    private readonly IHashService _hashService = hashService;
    private readonly AppDbContext _context = context;

    public async Task<RegisterDeviceResponse> RegisterDeviceAsync()
    {
        // Generate a new device ID and API key
        var deviceId = Guid.NewGuid();
        var apiKey = GenerateApiKey();
        var apiKeyHash = _hashService.Hash(apiKey); // BCrypt
        var apiKeySha256 = ComputeSha256(apiKey);   // SHA‑256

        var device = new Device
        {
            Id = deviceId,
            ApiKeyHash = apiKeyHash,
            ApiKeySha256 = apiKeySha256,
            CreatedAt = DateTime.UtcNow,
            // UserName and Colour can be set later by the user
        };

        _deviceRepo.Add(device);
        await _context.SaveChangesAsync();

        return new RegisterDeviceResponse
        {
            DeviceId = deviceId,
            ApiKey = apiKey
        };
    }

    public async Task<Device?> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return null;

        var sha256 = ComputeSha256(apiKey);
        var device = await _deviceRepo.GetByApiKeySha256Async(sha256);
        if (device == null)
            return null;

        if (!_hashService.Verify(apiKey, device.ApiKeyHash))
            return null;

        return device;
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeSha256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }


}