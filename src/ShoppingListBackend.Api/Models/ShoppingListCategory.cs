namespace ShoppingListBackend.Api.Models;

public class ShoppingListCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Position { get; set; } // -1 if not set

    public Guid ShoppingListId { get; set; }
    public ShoppingList ShoppingList { get; set; } = null!;

    public ICollection<ShoppingListItem> Items { get; set; } = [];
}
