namespace ShoppingListBackend.Api.DTOs.ShoppingList.Response;

public class ShoppingListCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Position { get; set; }
    public List<ShoppingListItemDto> Items { get; set; } = new();
}
