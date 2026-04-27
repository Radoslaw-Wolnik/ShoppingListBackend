using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ItemUpdatedEvent : ShoppingListEvent
{
    public ItemUpdatedEvent() => EventType = "ItemUpdated";
    public Guid ItemId { get; set; }
    public string Description { get; set; } = null!;
    public bool IsChecked { get; set; }
}
