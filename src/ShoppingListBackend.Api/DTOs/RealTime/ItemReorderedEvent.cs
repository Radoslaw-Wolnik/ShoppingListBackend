using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ItemReorderedEvent : ShoppingListEvent
{
    public ItemReorderedEvent() => EventType = "ItemReordered";
    public Guid CategoryId { get; set; }
    public List<ShoppingListItemDto> Items { get; set; } = [];
}
