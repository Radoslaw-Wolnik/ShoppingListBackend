using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class CategoryUpdatedEvent : ShoppingListEvent
{
    public CategoryUpdatedEvent() => EventType = "CategoryUpdated";
    public Guid CategoryId { get; set; }
    public string Name { get; set; }
}