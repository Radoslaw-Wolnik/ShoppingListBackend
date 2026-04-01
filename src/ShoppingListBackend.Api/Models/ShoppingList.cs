namespace ShoppingListBackend.Api.Models;

public class ShoppingList
{
    public Guid Id { get; set; }
    public string Title { get; set; }

    public Guid OwnerDeviceId { get; set; }
    public Device Owner { get; set; }
    public bool IsPrivate { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Categories (one-to-many)
    public ICollection<ShoppingListCategory> Categories { get; set; }

    // Editors (many-to-many)
    public ICollection<Device> Editors { get; set; }
}