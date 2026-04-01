namespace ShoppingListBackend.Api.Hubs;

[Authorize(AuthenticationSchemes = "ApiKey")]
public class ShoppingListHub : Hub
{
    private readonly IShoppingListService _shoppingListService;
    private readonly IShoppingListReadRepository _readRepository;

    public ShoppingListHub(IShoppingListService shoppingListService, IShoppingListReadRepository readRepository)
    {
        _shoppingListService = shoppingListService;
        _readRepository = readRepository;
    }

    private Guid GetDeviceId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var deviceId))
            throw new HubException("Device ID not found in claims");
        return deviceId;
    }

    public async Task JoinList(Guid listId)
    {
        var deviceId = GetDeviceId();

        // Verify that the device has access to this list
        var summaries = await _readRepository.GetSummariesForDeviceAsync(deviceId, CancellationToken.None);
        if (!summaries.Any(s => s.Id == listId))
            throw new HubException("You don't have access to this list");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"list-{listId}");
    }

    public async Task LeaveList(Guid listId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"list-{listId}");
    }

    // Optional: provide methods for real-time operations via hub (if you want to bypass HTTP)
    // These methods will call the same service and the service will broadcast.
    // But note: the service already broadcasts, so these are just thin wrappers.
    public async Task AddItem(Guid categoryId, string description)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.AddItemAsync(categoryId, deviceId, description);
    }

    public async Task ToggleItem(Guid itemId, bool isChecked)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.ToggleItemCheckedAsync(itemId, deviceId, isChecked);
    }

    // Add other methods as needed (ReorderItem, DeleteItem, etc.)
}