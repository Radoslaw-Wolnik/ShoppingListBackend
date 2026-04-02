namespace ShoppingListBackend.Api.DTOs.Device;

public class RegisterDeviceResponse
{
    public Guid DeviceId { get; set; }
    public string ApiKey { get; set; }
}