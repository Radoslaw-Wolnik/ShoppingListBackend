namespace ShoppingListBackend.Api.DTOs.ShoppingList.Request;

public class ToggleItemRequest
{
    // public guid ItemId
    public bool IsChecked { get; set; }
}