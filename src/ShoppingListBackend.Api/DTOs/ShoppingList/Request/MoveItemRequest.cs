namespace ShoppingListBackend.Api.DTOs.ShoppingList.Request;

public class MoveItemRequest
{
    // public guid ItemId
    public Guid NewCategoryId { get; set; }
}
