using ShoppingListBackend.Api.DTOs.ShoppingList;

namespace ShoppingListBackend.Api.DTOs.Sync;

public class SyncResponse
{
    public List<DeviceShoppingListHeader> UpdatedLists { get; set; } = new();
    public List<Guid> DeletedListIds { get; set; } = new();
    // maybe also a flag for more data needed
}