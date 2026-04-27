namespace ShoppingListBackend.Api.DTOs.Device;

public class FriendDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Colour { get; set; } = null!;
}
