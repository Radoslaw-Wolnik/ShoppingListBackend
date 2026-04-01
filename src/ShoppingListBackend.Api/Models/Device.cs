namespace ShoppingListBackend.Api.Models;

public class Device
{
    public Guid Id { get; set; } // Device identifier (public)
    public string ApiKeyHash { get; set; } // Hashed API key
    public string ApiKeySha256 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // user info
    public string UserName { get; set; }
    public string Colour { get; set; }

    // Owned lists
    public ICollection<ShoppingList> OwnedShoppingLists { get; set; }

    // Lists where this device is an editor (many-to-many)
    public ICollection<ShoppingList> EditableShoppingLists { get; set; }

    // Friends (many-to-many self)
    public ICollection<DeviceFriend> Friends { get; set; }

}