namespace ShoppingListBackend.Api.DTOs.Common;

public class DeviceInfo
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Colour { get; set; } = null!;
}
