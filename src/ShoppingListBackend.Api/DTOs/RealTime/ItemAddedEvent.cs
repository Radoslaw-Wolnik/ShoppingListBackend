using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ItemAddedEvent : ShoppingListEvent
{
    public ItemAddedEvent() => EventType = "ItemAdded";
    public ShoppingListItemDto Item { get; set; } = null!;
}
