namespace ShoppingListBackend.Api.Services;

public interface IDeviceService
{
    Task<Device> GetDeviceAsync(Guid deviceId);
    Task UpdateUsernameAsync(Guid deviceId, string newUsername);
    Task UpdateColourAsync(Guid deviceId, string newColour);
    Task AddFriendAsync(Guid deviceId, Guid friendId);
    Task RemoveFriendAsync(Guid deviceId, Guid friendId);
    Task DeleteDeviceAsync(Guid deviceId);
}