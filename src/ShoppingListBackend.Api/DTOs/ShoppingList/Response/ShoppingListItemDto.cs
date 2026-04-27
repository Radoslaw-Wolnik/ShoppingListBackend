namespace ShoppingListBackend.Api.DTOs.ShoppingList.Response;

public class ShoppingListItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = null!;
    public bool IsChecked { get; set; }
    public int Position { get; set; }
    public Guid CategoryId { get; set; }
}
