namespace ShoppingListBackend.Api.Models;

public class EditingSession
{
    public Guid ListId { get; set; }
    public Guid DeviceId { get; set; }
    public string ConnectionId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Colour { get; set; } = null!;
    public DateTime LastSeenAt { get; set; }
}
