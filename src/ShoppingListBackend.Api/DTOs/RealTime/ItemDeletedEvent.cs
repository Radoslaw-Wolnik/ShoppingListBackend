using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ItemDeletedEvent : ShoppingListEvent
{
    public ItemDeletedEvent() => EventType = "ItemDeleted";
    public Guid ItemId { get; set; }
}