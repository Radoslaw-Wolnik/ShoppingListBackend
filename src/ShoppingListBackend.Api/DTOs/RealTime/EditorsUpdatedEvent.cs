using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class EditorsUpdatedEvent : ShoppingListEvent
{
    public EditorsUpdatedEvent() => EventType = "EditorsUpdated";
    public List<DeviceInfo> Editors { get; set; } = [];
}
