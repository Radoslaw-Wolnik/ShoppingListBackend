namespace ShoppingListBackend.Api.Repositories;

// Repositories/ShoppingListRepository.cs
public class ShoppingListRepository(AppDbContext context) : IShoppingListRepository
{
    private readonly AppDbContext _context = context;

    public async Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ShoppingLists
            .Include(s => s.Owner)
            .Include(s => s.Editors)
            .Include(s => s.Categories)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<ShoppingListCategory?> GetCategoryByIdAsync(Guid categoryId, CancellationToken ct = default)
        => await _context.ShoppingListCategories
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

    public async Task<ShoppingListItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default)
        => await _context.ShoppingListItems
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

    public void Add(ShoppingList list)
    {
        list.Id = Guid.NewGuid();
        list.CreatedAt = DateTime.UtcNow;
        _context.ShoppingLists.Add(list);
    }

    public void Delete(ShoppingList list) => _context.ShoppingLists.Remove(list);

    public void AddCategory(ShoppingListCategory category)
    {
        category.Id = Guid.NewGuid();
        _context.ShoppingListCategories.Add(category);
    }

    public void DeleteCategory(ShoppingListCategory category) => _context.ShoppingListCategories.Remove(category);

    public void AddItem(ShoppingListItem item)
    {
        item.Id = Guid.NewGuid();
        _context.ShoppingListItems.Add(item);
    }

    public void DeleteItem(ShoppingListItem item) => _context.ShoppingListItems.Remove(item);

    public IQueryable<ShoppingListItem> GetListItemsQuery(Guid listId)
        => from i in _context.ShoppingListItems
           join c in _context.ShoppingListCategories on i.ShoppingListCategoryId equals c.Id
           where c.ShoppingListId == listId
           select i;
}
