using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.Services;

public interface IEditingTracker
{
    Task AddDeviceAsync(Guid listId, DeviceInfo device, string connectionId, CancellationToken ct = default);
    Task RemoveDeviceAsync(Guid listId, Guid deviceId, CancellationToken ct = default);
    Task<IReadOnlyCollection<Guid>> RemoveConnectionAsync(string connectionId, CancellationToken ct = default);
    Task<List<DeviceInfo>> GetEditingDevicesAsync(Guid listId, CancellationToken ct = default);
}
