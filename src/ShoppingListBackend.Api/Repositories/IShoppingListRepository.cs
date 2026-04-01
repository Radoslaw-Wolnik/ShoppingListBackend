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
    // For batch operations like resetting checked items, we provide a query method
    IQueryable<ShoppingListItem> GetListItemsQuery(Guid listId);
}
