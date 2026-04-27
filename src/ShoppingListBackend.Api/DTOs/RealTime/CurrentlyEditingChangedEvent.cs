using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class CurrentlyEditingChangedEvent : ShoppingListEvent
{
    public CurrentlyEditingChangedEvent() => EventType = "CurrentlyEditingChanged";
    public List<DeviceInfo> EditingDevices { get; set; } = [];
}
