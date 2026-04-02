using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class CategoryDeletedEvent : ShoppingListEvent
{
    public CategoryDeletedEvent() => EventType = "CategoryDeleted";
    public Guid CategoryId { get; set; }
}