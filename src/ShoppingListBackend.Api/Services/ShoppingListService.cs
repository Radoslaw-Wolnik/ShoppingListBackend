namespace ShoppingListBackend.Api.Services;

public class ShoppingListService(
    IShoppingListRepository repo,
    AppDbContext context,
    IHubContext<ShoppingListHub> hubContext) : IShoppingListService
{
    private readonly IShoppingListRepository _repo = repo;
    private readonly AppDbContext _context = context;
    private readonly IHubContext<ShoppingListHub> _hubContext = hubContext;

    // Helper to broadcast to a list's group
    private async Task BroadcastAsync<T>(Guid listId, T @event) where T : ShoppingListEvent
    {
        @event.ListId = listId;
        await _hubContext.Clients.Group($"list-{listId}").SendAsync("ShoppingListEvent", @event);
    }


    // Helper to check permission
    private async Task<ShoppingList> GetAndAuthorizeAsync(Guid listId, Guid requesterId, bool requireOwner = false)
    {
        var list = await _repo.GetByIdAsync(listId);
        if (list == null) throw new KeyNotFoundException("List not found");

        bool isOwner = list.OwnerDeviceId == requesterId;
        bool isEditor = list.Editors.Any(e => e.Id == requesterId);

        if (requireOwner && !isOwner)
            throw new UnauthorizedAccessException("Only the owner can perform this action");
        if (!isOwner && !isEditor)
            throw new UnauthorizedAccessException("You don't have permission to modify this list");

        return list;
    }

    public async Task<ShoppingList> CreateListAsync(Guid ownerId, string title)
    {
        var list = new ShoppingList
        {
            Title = title,
            OwnerDeviceId = ownerId,
            CreatedAt = DateTime.UtcNow,
            Categories = new List<ShoppingListCategory>()
        };
        _repo.Add(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task UpdateListTitleAsync(Guid listId, Guid requesterId, string newTitle)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: true);
        list.Title = newTitle;
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        // brodcast signalR change
        await BroadcastAsync(listId, new ListUpdatedEvent { Title = newTitle });
    }

    public async Task DeleteListAsync(Guid listId, Guid requesterId)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: true);
        _repo.Delete(list);
        await _context.SaveChangesAsync();

        await BroadcastAsync(listId, new ListDeletedEvent());
    }

    public async Task<ShoppingList> CopyListAsync(Guid sourceId, Guid requesterId)
    {
        var source = await _repo.GetByIdAsync(sourceId);
        if (source == null) throw new KeyNotFoundException("Source list not found");

        // Check permission to copy (anyone who can view can copy)
        var isOwner = source.OwnerDeviceId == requesterId;
        var isEditor = source.Editors.Any(e => e.Id == requesterId);
        if (!isOwner && !isEditor) throw new UnauthorizedAccessException();

        var newList = new ShoppingList
        {
            Title = $"{source.Title} (copy)",
            OwnerDeviceId = requesterId,
            CreatedAt = DateTime.UtcNow,
            Categories = new List<ShoppingListCategory>()
        };
        _repo.Add(newList);
        await _context.SaveChangesAsync(); // Save to get ID, but we still need to add items

        // Deep copy categories and items
        foreach (var sourceCategory in source.Categories.OrderBy(c => c.Position))
        {
            var newCategory = new ShoppingListCategory
            {
                ShoppingListId = newList.Id,
                Name = sourceCategory.Name,
                Position = sourceCategory.Position,
                Items = new List<ShoppingListItem>()
            };
            _repo.AddCategory(newCategory);
            await _context.SaveChangesAsync(); // To get category ID

            foreach (var sourceItem in sourceCategory.Items.OrderBy(i => i.Position))
            {
                var newItem = new ShoppingListItem
                {
                    ShoppingListCategoryId = newCategory.Id,
                    Description = sourceItem.Description,
                    IsChecked = false,
                    Position = sourceItem.Position
                };
                _repo.AddItem(newItem);
            }
        }
        await _context.SaveChangesAsync();
        return newList;
    }

    public async Task AddEditorAsync(Guid listId, Guid requesterId, Guid newEditorId)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: true);
        if (list.Editors.Any(e => e.Id == newEditorId)) return;

        var editor = await _context.Devices.FindAsync(newEditorId);
        if (editor == null) throw new KeyNotFoundException("Editor device not found");

        list.Editors.Add(editor);
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Broadcast the updated editor list
        var editorDtos = list.Editors.Select(e => new DeviceDto { Id = e.Id, UserName = e.UserName, Colour = e.Colour }).ToList();
        await BroadcastAsync(listId, new EditorsUpdatedEvent { Editors = editorDtos });
    }

    public async Task RemoveEditorAsync(Guid listId, Guid requesterId, Guid editorId)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: true);
        var editor = list.Editors.FirstOrDefault(e => e.Id == editorId);
        if (editor != null)
        {
            list.Editors.Remove(editor);
            list.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var editorDtos = list.Editors.Select(e => new DeviceDto { Id = e.Id, UserName = e.UserName, Colour = e.Colour }).ToList();
            await BroadcastAsync(listId, new EditorsUpdatedEvent { Editors = editorDtos });
        }
    }

    public async Task AddCategoryAsync(Guid listId, Guid requesterId, string categoryName)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: false);
        var newCategory = new ShoppingListCategory
        {
            ShoppingListId = listId,
            Name = categoryName,
            Position = list.Categories.Count // append at end
        };
        _repo.AddCategory(newCategory);
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var categoryDto = MapCategoryDto(newCategory);
        await BroadcastAsync(listId, new CategoryAddedEvent { Category = categoryDto });
    }

    public async Task UpdateCategoryNameAsync(Guid categoryId, Guid requesterId, string newName)
    {
        var category = await _repo.GetCategoryByIdAsync(categoryId);
        if (category == null) throw new KeyNotFoundException();

        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        category.Name = newName;
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastAsync(list.Id, new CategoryUpdatedEvent { CategoryId = categoryId, Name = newName });
    }

    public async Task DeleteCategoryAsync(Guid categoryId, Guid requesterId)
    {
        var category = await _repo.GetCategoryByIdAsync(categoryId);
        if (category == null) throw new KeyNotFoundException();

        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        _repo.DeleteCategory(category);
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastAsync(list.Id, new CategoryDeletedEvent { CategoryId = categoryId });
    }

    public async Task ReorderCategoryAsync(Guid categoryId, Guid requesterId, int newPosition)
    {
        var category = await _repo.GetCategoryByIdAsync(categoryId);
        if (category == null) throw new KeyNotFoundException();

        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        var categories = list.Categories.OrderBy(c => c.Position).ToList();
        var target = categories.FirstOrDefault(c => c.Id == categoryId);
        if (target == null) return;

        categories.Remove(target);
        categories.Insert(newPosition, target);
        for (int i = 0; i < categories.Count; i++)
            categories[i].Position = i;

        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Broadcast new order
        var categoryDtos = categories.Select(MapCategoryDto).ToList();
        await BroadcastAsync(list.Id, new CategoryReorderedEvent { Categories = categoryDtos });
    }

    public async Task AddItemAsync(Guid categoryId, Guid requesterId, string description)
    {
        var category = await _repo.GetCategoryByIdAsync(categoryId);
        if (category == null) throw new KeyNotFoundException();

        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        var newItem = new ShoppingListItem
        {
            ShoppingListCategoryId = categoryId,
            Description = description,
            IsChecked = false,
            Position = category.Items.Count // append at end
        };
        _repo.AddItem(newItem);
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var itemDto = MapItemDto(newItem);
        await BroadcastAsync(list.Id, new ItemAddedEvent { Item = itemDto });
    }

    public async Task UpdateItemDescriptionAsync(Guid itemId, Guid requesterId, string newDescription)
    {
        var item = await _repo.GetItemByIdAsync(itemId);
        if (item == null) throw new KeyNotFoundException();

        var category = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId);
        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        item.Description = newDescription;
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastAsync(list.Id, new ItemUpdatedEvent { ItemId = itemId, Description = newDescription, IsChecked = item.IsChecked });
    }

    public async Task ToggleItemCheckedAsync(Guid itemId, Guid requesterId, bool isChecked)
    {
        var item = await _repo.GetItemByIdAsync(itemId);
        if (item == null) throw new KeyNotFoundException();

        var category = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId);
        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        item.IsChecked = isChecked;
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastAsync(list.Id, new ItemToggledEvent { ItemId = itemId, IsChecked = isChecked });
    }

    public async Task DeleteItemAsync(Guid itemId, Guid requesterId)
    {
        var item = await _repo.GetItemByIdAsync(itemId);
        if (item == null) throw new KeyNotFoundException();

        var category = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId);
        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        _repo.DeleteItem(item);
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastAsync(list.Id, new ItemDeletedEvent { ItemId = itemId });
    }

    public async Task ReorderItemAsync(Guid categoryId, Guid requesterId, Guid itemId, int newPosition)
    {
        var category = await _repo.GetCategoryByIdAsync(categoryId);
        if (category == null) throw new KeyNotFoundException();

        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        var items = category.Items.OrderBy(i => i.Position).ToList();
        var target = items.FirstOrDefault(i => i.Id == itemId);
        if (target == null) throw new KeyNotFoundException();

        items.Remove(target);
        items.Insert(newPosition, target);
        for (int i = 0; i < items.Count; i++)
            items[i].Position = i;

        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var itemDtos = items.Select(MapItemDto).ToList();
        await BroadcastAsync(list.Id, new ItemReorderedEvent { CategoryId = categoryId, Items = itemDtos });
    }

    public async Task MoveItemToCategoryAsync(Guid itemId, Guid requesterId, Guid newCategoryId)
    {
        var item = await _repo.GetItemByIdAsync(itemId);
        if (item == null) throw new KeyNotFoundException();

        var oldCategory = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId);
        var newCategory = await _repo.GetCategoryByIdAsync(newCategoryId);
        if (newCategory == null) throw new KeyNotFoundException();

        var list = await GetAndAuthorizeAsync(oldCategory.ShoppingListId, requesterId, requireOwner: false);
        // Check that new category belongs to same list
        if (newCategory.ShoppingListId != list.Id)
            throw new InvalidOperationException("Cannot move item to a category from a different list");

        // Remove from old order
        var oldItems = oldCategory.Items.OrderBy(i => i.Position).ToList();
        oldItems.Remove(item);
        for (int i = 0; i < oldItems.Count; i++)
            oldItems[i].Position = i;

        // Add to new category
        item.ShoppingListCategoryId = newCategoryId;
        var newItems = newCategory.Items.OrderBy(i => i.Position).ToList();
        item.Position = newItems.Count;
        newItems.Add(item);
        // (no need to reorder new items unless we insert at a specific position)

        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // brodcast update properly
        
    }

    public async Task ResetCheckedItemsAsync(Guid listId, Guid requesterId)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: false);
        var itemsToReset = await _repo.GetListItemsQuery(listId)
            .Where(i => i.IsChecked)
            .ToListAsync();
        foreach (var item in itemsToReset)
            item.IsChecked = false;
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}