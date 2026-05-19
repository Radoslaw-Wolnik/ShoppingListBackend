using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.Services;

public interface IEditingTracker
{
    void AddDevice(Guid listId, DeviceInfo device, string connectionId);
    void RemoveDevice(Guid listId, Guid deviceId);
    IReadOnlyCollection<Guid> RemoveConnection(string connectionId);
    List<DeviceInfo> GetEditingDevices(Guid listId);
}
