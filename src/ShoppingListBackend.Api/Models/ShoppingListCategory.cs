namespace ShoppingListBackend.Api.Models;

public class ShoppingListCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Position { get; set; } // -1 if not set

    public Guid ShoppingListId { get; set; }
    public ShoppingList ShoppingList { get; set; }

    public ICollection<ShoppingListItem> Items { get; set; }
}