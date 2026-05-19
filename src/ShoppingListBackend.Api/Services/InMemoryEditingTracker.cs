using System.Collections.Concurrent;
using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.Services;

public class InMemoryEditingTracker : IEditingTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, (DeviceInfo Info, string ConnectionId)>> _tracker = new();

    public void AddDevice(Guid listId, DeviceInfo device, string connectionId)
    {
        var listDevices = _tracker.GetOrAdd(listId, _ => new ConcurrentDictionary<Guid, (DeviceInfo, string)>());
        listDevices.AddOrUpdate(device.Id, (device, connectionId), (_, _) => (device, connectionId));
    }

    public void RemoveDevice(Guid listId, Guid deviceId)
    {
        if (_tracker.TryGetValue(listId, out var listDevices))
            listDevices.TryRemove(deviceId, out _);
    }

    public IReadOnlyCollection<Guid> RemoveConnection(string connectionId)
    {
        var affectedLists = new List<Guid>();
        foreach (var (listId, listDevices) in _tracker)
        {
            var toRemove = listDevices.FirstOrDefault(kvp => kvp.Value.ConnectionId == connectionId);
            if (toRemove.Key != Guid.Empty)
            {
                listDevices.TryRemove(toRemove.Key, out _);
                affectedLists.Add(listId);
            }
        }

        return affectedLists;
    }

    public List<DeviceInfo> GetEditingDevices(Guid listId)
    {
        if (_tracker.TryGetValue(listId, out var listDevices))
            return listDevices.Values.Select(v => v.Info).ToList();
        return new List<DeviceInfo>();
    }
}
