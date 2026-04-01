namespace ShoppingListBackend.Api.Repositories;

public class DeviceRepository(AppDbContext context) : IDeviceRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Devices.FindAsync(new object[] { id }, ct);

    public async Task<Device?> GetByApiKeySha256Async(string sha256, CancellationToken ct = default)
        => await _context.Devices.FirstOrDefaultAsync(d => d.ApiKeySha256 == sha256, ct);

    public void Add(Device device) => _context.Devices.Add(device);

    public void Update(Device device) => _context.Devices.Update(device);

    public void Delete(Device device) => _context.Devices.Remove(device);

    public void AddFriend(Guid deviceId, Guid friendId)
        => _context.DeviceFriends.Add(new DeviceFriend { DeviceId = deviceId, FriendId = friendId });

    public void RemoveFriend(Guid deviceId, Guid friendId)
    {
        var friendship = _context.DeviceFriends.Local
            .FirstOrDefault(df => df.DeviceId == deviceId && df.FriendId == friendId);
        if (friendship != null)
        {
            _context.DeviceFriends.Remove(friendship);
        }
        else
        {
            // If not tracked, attach and mark as deleted
            var entity = new DeviceFriend { DeviceId = deviceId, FriendId = friendId };
            _context.DeviceFriends.Attach(entity);
            _context.DeviceFriends.Remove(entity);
        }
    }

    public IQueryable<Device> GetFriends(Guid deviceId)
        => _context.DeviceFriends
            .Where(df => df.DeviceId == deviceId)
            .Select(df => df.Friend);

}
