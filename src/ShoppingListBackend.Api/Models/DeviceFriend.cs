namespace ShoppingListBackend.Api.Models;

public class DeviceFriend
{
    public Guid DeviceId { get; set; }
    public Device Device { get; set; }
    public Guid FriendId { get; set; }
    public Device Friend { get; set; }
}