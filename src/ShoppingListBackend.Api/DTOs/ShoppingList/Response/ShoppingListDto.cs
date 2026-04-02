using ShoppingListBackend.Api.DTOs.Common;

namespace ShoppingListBackend.Api.DTOs.ShoppingList.Response;

public class ShoppingListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public DeviceInfo Owner { get; set; }
    public List<DeviceInfo> Editors { get; set; } = new();
    public List<ShoppingListCategoryDto> Categories { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}