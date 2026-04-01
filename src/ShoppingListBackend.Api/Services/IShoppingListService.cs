namespace ShoppingListBackend.Api.Services;

public interface IShoppingListService
{
    Task<ShoppingList> CreateListAsync(Guid ownerId, string title);
    Task UpdateListTitleAsync(Guid listId, Guid requesterId, string newTitle);
    Task DeleteListAsync(Guid listId, Guid requesterId);
    Task<ShoppingList> CopyListAsync(Guid sourceId, Guid requesterId);
    Task AddEditorAsync(Guid listId, Guid requesterId, Guid newEditorId);
    Task RemoveEditorAsync(Guid listId, Guid requesterId, Guid editorId);
    
    Task AddCategoryAsync(Guid listId, Guid requesterId, string categoryName);
    Task UpdateCategoryNameAsync(Guid categoryId, Guid requesterId, string newName);
    Task DeleteCategoryAsync(Guid categoryId, Guid requesterId);
    Task ReorderCategoryAsync(Guid categoryId, Guid requesterId, int newPosition);
    
    Task AddItemAsync(Guid categoryId, Guid requesterId, string description);
    Task UpdateItemDescriptionAsync(Guid itemId, Guid requesterId, string newDescription);
    Task ToggleItemCheckedAsync(Guid itemId, Guid requesterId, bool isChecked);
    Task DeleteItemAsync(Guid itemId, Guid requesterId);
    Task ReorderItemAsync(Guid categoryId, Guid requesterId, Guid itemId, int newPosition);
    Task MoveItemToCategoryAsync(Guid itemId, Guid requesterId, Guid newCategoryId);
    
    Task ResetCheckedItemsAsync(Guid listId, Guid requesterId);
}
