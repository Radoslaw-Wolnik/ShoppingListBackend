namespace ShoppingListBackend.Api.Models;

public class DeviceFriend
{
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public Guid FriendId { get; set; }
    public Device Friend { get; set; } = null!;

    // timestamps
    public DateTime CreatedAt { get; set; }
}
