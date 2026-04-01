namespace ShoppingListBackend.Api.Models;

public class ShoppingListItem
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public bool IsChecked { get; set; }
    public int Position { get; set; }

    public Guid ShoppingListCategoryId { get; set; }
    public ShoppingListCategory ShoppingListCategory { get; set; }
}