namespace ShoppingListBackend.Api.DTOs.ShoppingList.Request;

public class ReorderItemRequest
{
    // public guid ItemId
    public int NewPosition { get; set; }
}
