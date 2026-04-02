namespace ShoppingListBackend.Api.DTOs.ShoppingList.Request;

public class ReorderCategoryRequest
{
    // public guid CategoryId
    public int NewPosition { get; set; }
}