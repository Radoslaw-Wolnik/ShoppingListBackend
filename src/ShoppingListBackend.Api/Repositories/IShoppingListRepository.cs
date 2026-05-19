using ShoppingListBackend.Api.Models;

namespace ShoppingListBackend.Api.Repositories;

public interface IShoppingListRepository
{
    Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ShoppingListCategory?> GetCategoryByIdAsync(Guid categoryId, CancellationToken ct = default);
    Task<ShoppingListItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default);
    void Add(ShoppingList list);
    void Delete(ShoppingList list);
    void AddCategory(ShoppingListCategory category);
    void DeleteCategory(ShoppingListCategory category);
    void AddItem(ShoppingListItem item);
    void DeleteItem(ShoppingListItem item);
    Task<List<ShoppingListItem>> GetCheckedItemsForListAsync(Guid listId, CancellationToken ct = default);
}
