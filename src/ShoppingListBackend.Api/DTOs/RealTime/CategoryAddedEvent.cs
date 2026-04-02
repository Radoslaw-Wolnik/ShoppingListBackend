using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.ShoppingList;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class CategoryAddedEvent : ShoppingListEvent
{
    public CategoryAddedEvent() => EventType = "CategoryAdded";
    public ShoppingListCategoryDto Category { get; set; }
}