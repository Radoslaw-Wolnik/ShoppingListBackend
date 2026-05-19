using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using ShoppingListBackend.Api.Data;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.RealTime;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;
using ShoppingListBackend.Api.Hubs;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Repositories;

namespace ShoppingListBackend.Api.Services;

public class ShoppingListService(
    IShoppingListRepository repo,
    AppDbContext context,
    IHubContext<ShoppingListHub> hubContext,
    IMapper mapper) : IShoppingListService
{
    private readonly IShoppingListRepository _repo = repo;
    private readonly AppDbContext _context = context;
    private readonly IHubContext<ShoppingListHub> _hubContext = hubContext;
    private readonly IMapper _mapper = mapper;

    private static int NextCategoryPosition(ShoppingList list)
        => list.Categories.Count == 0 ? 0 : list.Categories.Max(c => c.Position) + 1;

    private static int NextItemPosition(ShoppingListCategory category)
        => category.Items.Count == 0 ? 0 : category.Items.Max(i => i.Position) + 1;

    private static void ReindexCategories(IEnumerable<ShoppingListCategory> categories)
    {
        var ordered = categories.OrderBy(c => c.Position).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Position = i;
    }

    private static void ReindexItems(IEnumerable<ShoppingListItem> items)
    {
        var ordered = items.OrderBy(i => i.Position).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Position = i;
    }

    private async Task BroadcastAsync<T>(Guid listId, T @event) where T : ShoppingListEvent
    {
        @event.ListId = listId;
        await _hubContext.Clients.Group($"list-{listId}").SendAsync("ShoppingListEvent", @event);
    }

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
        await _context.SaveChangesAsync();

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
            await _context.SaveChangesAsync();

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

        var editorDtos = list.Editors.Select(e => _mapper.Map<DeviceInfo>(e)).ToList();
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
            var editorDtos = list.Editors.Select(e => _mapper.Map<DeviceInfo>(e)).ToList();
            await BroadcastAsync(listId, new EditorsUpdatedEvent { Editors = editorDtos });
        }
    }

    public async Task<ShoppingListCategory> AddCategoryAsync(Guid listId, Guid requesterId, string categoryName)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: false);
        var newCategory = new ShoppingListCategory
        {
            ShoppingListId = listId,
            Name = categoryName,
            Position = NextCategoryPosition(list)
        };
        _repo.AddCategory(newCategory);
        if (!list.Categories.Contains(newCategory))
            list.Categories.Add(newCategory);
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var categoryDto = _mapper.Map<ShoppingListCategoryDto>(newCategory);
        await BroadcastAsync(listId, new CategoryAddedEvent { Category = categoryDto });
        return newCategory;
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
        list.Categories.Remove(category);
        ReindexCategories(list.Categories);
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
        var insertAt = Math.Clamp(newPosition, 0, categories.Count);
        categories.Insert(insertAt, target);
        for (int i = 0; i < categories.Count; i++)
            categories[i].Position = i;

        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var categoryDtos = categories.Select(_mapper.Map<ShoppingListCategoryDto>).ToList();
        await BroadcastAsync(list.Id, new CategoryReorderedEvent { Categories = categoryDtos });
    }

    public async Task<ShoppingListItem> AddItemAsync(Guid categoryId, Guid requesterId, string description)
    {
        var category = await _repo.GetCategoryByIdAsync(categoryId);
        if (category == null) throw new KeyNotFoundException();

        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        var newItem = new ShoppingListItem
        {
            ShoppingListCategoryId = categoryId,
            Description = description,
            IsChecked = false,
            Position = NextItemPosition(category)
        };
        _repo.AddItem(newItem);
        if (!category.Items.Contains(newItem))
            category.Items.Add(newItem);
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var itemDto = _mapper.Map<ShoppingListItemDto>(newItem);
        await BroadcastAsync(list.Id, new ItemAddedEvent { Item = itemDto });
        return newItem;
    }

    public async Task UpdateItemDescriptionAsync(Guid itemId, Guid requesterId, string newDescription)
    {
        var item = await _repo.GetItemByIdAsync(itemId);
        if (item == null) throw new KeyNotFoundException();

        var category = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId) ?? throw new KeyNotFoundException("Category not found");
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

        var category = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId) ?? throw new KeyNotFoundException("Category not found");
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

        var category = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId) ?? throw new KeyNotFoundException("Category not found");
        var list = await GetAndAuthorizeAsync(category.ShoppingListId, requesterId, requireOwner: false);
        _repo.DeleteItem(item);
        category.Items.Remove(item);
        ReindexItems(category.Items);
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
        var insertAt = Math.Clamp(newPosition, 0, items.Count);
        items.Insert(insertAt, target);
        for (int i = 0; i < items.Count; i++)
            items[i].Position = i;

        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var itemDtos = items.Select(_mapper.Map<ShoppingListItemDto>).ToList();
        await BroadcastAsync(list.Id, new ItemReorderedEvent { CategoryId = categoryId, Items = itemDtos });
    }

    public async Task MoveItemToCategoryAsync(Guid itemId, Guid requesterId, Guid newCategoryId)
    {
        var item = await _repo.GetItemByIdAsync(itemId);
        if (item == null) throw new KeyNotFoundException();

        var oldCategory = await _repo.GetCategoryByIdAsync(item.ShoppingListCategoryId);
        var newCategory = await _repo.GetCategoryByIdAsync(newCategoryId);
        if (newCategory == null) throw new KeyNotFoundException();
        if (oldCategory == null) throw new KeyNotFoundException("Category not found");

        var list = await GetAndAuthorizeAsync(oldCategory.ShoppingListId, requesterId, requireOwner: false);
        if (newCategory.ShoppingListId != list.Id)
            throw new InvalidOperationException("Cannot move item to a category from a different list");
        if (oldCategory.Id == newCategory.Id)
            return;

        oldCategory.Items.Remove(item);
        ReindexItems(oldCategory.Items);

        item.ShoppingListCategoryId = newCategoryId;
        item.Position = NextItemPosition(newCategory);
        newCategory.Items.Add(item);
        ReindexItems(newCategory.Items);

        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await BroadcastAsync(list.Id, new ItemMovedEvent
        {
            ItemId = itemId,
            FromCategoryId = oldCategory.Id,
            ToCategoryId = newCategoryId,
            NewPosition = item.Position
        });
    }

    public async Task ResetCheckedItemsAsync(Guid listId, Guid requesterId)
    {
        var list = await GetAndAuthorizeAsync(listId, requesterId, requireOwner: false);
        var itemsToReset = await _repo.GetCheckedItemsForListAsync(listId);
        foreach (var item in itemsToReset)
            item.IsChecked = false;
        list.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await BroadcastAsync(listId, new ListItemsResetEvent());
    }
}
