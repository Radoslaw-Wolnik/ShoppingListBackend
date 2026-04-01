using ShoppingListBackend.Api.Models;

namespace ShoppingListBackend.Api.Repositories;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Device?> GetByApiKeySha256Async(string sha256, CancellationToken ct = default);
    void Add(Device device);
    void Update(Device device);
    void Delete(Device device);
    void AddFriend(Guid deviceId, Guid friendId);
    void RemoveFriend(Guid deviceId, Guid friendId);
    IQueryable<Device> GetFriends(Guid deviceId);
}
