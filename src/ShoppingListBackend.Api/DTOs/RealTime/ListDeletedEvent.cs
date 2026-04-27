using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ListDeletedEvent : ShoppingListEvent
{
    public ListDeletedEvent() => EventType = "ListDeleted";
}
