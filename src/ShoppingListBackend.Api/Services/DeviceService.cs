namespace ShoppingListBackend.Api.Services;

public class DeviceService(IDeviceRepository deviceRepo, AppDbContext context) : IDeviceService
{
    private readonly IDeviceRepository _deviceRepo = deviceRepo;
    private readonly AppDbContext _context = context;

    public async Task<Device> GetDeviceAsync(Guid deviceId)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId);
        if (device == null)
            throw new KeyNotFoundException($"Device {deviceId} not found");
        return device;
    }

    public async Task UpdateUsernameAsync(Guid deviceId, string newUsername)
    {
        var device = await GetDeviceAsync(deviceId);
        device.UserName = newUsername;
        _deviceRepo.Update(device);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateColourAsync(Guid deviceId, string newColour)
    {
        var device = await GetDeviceAsync(deviceId);
        device.Colour = newColour;
        _deviceRepo.Update(device);
        await _context.SaveChangesAsync();
    }

    public async Task AddFriendAsync(Guid deviceId, Guid friendId)
    {
        if (deviceId == friendId)
            throw new InvalidOperationException("Cannot add self as friend");

        // Ensure both devices exist
        var device = await GetDeviceAsync(deviceId);
        var friend = await _deviceRepo.GetByIdAsync(friendId);
        if (friend == null)
            throw new KeyNotFoundException($"Friend device {friendId} not found");

        // Check if already friends – if not, add
        var existing = await _deviceRepo.GetFriends(deviceId).AnyAsync(f => f.Id == friendId);
        if (existing) return;

        _deviceRepo.AddFriend(deviceId, friendId);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveFriendAsync(Guid deviceId, Guid friendId)
    {
        // Optional: verify friendship exists before removing
        await _deviceRepo.RemoveFriendAsync(deviceId, friendId);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDeviceAsync(Guid deviceId)
    {
        var device = await GetDeviceAsync(deviceId);
        // Optionally check if device owns any lists and decide policy
        _deviceRepo.Delete(device);
        await _context.SaveChangesAsync();
    }


    public async Task<IEnumerable<Device>> GetFriendsAsync(Guid deviceId)
    {
        return await _deviceRepo.GetFriends(deviceId).ToListAsync();
    }
}