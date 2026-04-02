using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ItemToggledEvent : ShoppingListEvent
{
    public ItemToggledEvent() => EventType = "ItemToggled";
    public Guid ItemId { get; set; }
    public bool IsChecked { get; set; }
}