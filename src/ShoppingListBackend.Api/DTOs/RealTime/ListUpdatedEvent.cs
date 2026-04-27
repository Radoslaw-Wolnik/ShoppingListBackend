using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class ListUpdatedEvent : ShoppingListEvent
{
    public ListUpdatedEvent() => EventType = "ListUpdated";
    public string Title { get; set; } = null!;
}
