using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.ShoppingList.Response;

public class DeviceShoppingListHeader
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public DeviceInfo Owner { get; set; }
    public List<DeviceInfo> Editors { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}