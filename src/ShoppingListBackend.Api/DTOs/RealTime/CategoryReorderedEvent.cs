using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;

namespace ShoppingListBackend.Api.DTOs.RealTime;

public class CategoryReorderedEvent : ShoppingListEvent
{
    public CategoryReorderedEvent() => EventType = "CategoryReordered";
    public List<ShoppingListCategoryDto> Categories { get; set; } = [];
}
