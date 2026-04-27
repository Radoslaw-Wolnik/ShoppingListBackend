namespace ShoppingListBackend.Api.Models;

public class Device
{
    public Guid Id { get; set; } // Device identifier (public)
    public string ApiKeyHash { get; set; } = null!; // Hashed API key
    public string ApiKeySha256 { get; set; } = null!;
    
    // user info
    public string UserName { get; set; } = null!;
    public string Colour { get; set; } = null!;
    public string? FriendCode { get; set; }

    // timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? FriendCodeCreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Owned lists
    public ICollection<ShoppingList> OwnedShoppingLists { get; set; } = [];
    // Lists where this device is an editor (many-to-many)
    public ICollection<ShoppingList> EditableShoppingLists { get; set; } = [];

    // Friends (many-to-many self)
    public ICollection<DeviceFriend> DeviceFriends { get; set; } = new List<DeviceFriend>();
    public ICollection<DeviceFriend> FriendOf { get; set; } = new List<DeviceFriend>();

}