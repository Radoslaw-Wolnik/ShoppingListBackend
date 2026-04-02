using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ListItemsResetEvent : ShoppingListEvent
{
    public ListItemsResetEvent() => EventType = "ListItemsReset";
}