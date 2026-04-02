using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ItemMovedEvent : ShoppingListEvent
{
    public ItemMovedEvent() => EventType = "ItemMoved";
    public Guid ItemId { get; set; }
    public Guid FromCategoryId { get; set; }
    public Guid ToCategoryId { get; set; }
    public int NewPosition { get; set; }
}