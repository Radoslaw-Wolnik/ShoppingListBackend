namespace ShoppingListBackend.Api.DTOs.Common;

public abstract class ShoppingListEvent
{
    public string EventType { get; set; } = null!;
    public Guid ListId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
