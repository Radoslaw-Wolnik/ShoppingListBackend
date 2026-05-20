using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.RealTime;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Api.Data;

namespace ShoppingListBackend.Api.Hubs;

[Authorize(AuthenticationSchemes = "ApiKey")]
public class ShoppingListHub : Hub
{
    private readonly IShoppingListService _shoppingListService;
    private readonly IShoppingListReadService _readService;
    private readonly IEditingTracker _editingTracker;
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public ShoppingListHub(
        IShoppingListService shoppingListService,
        IShoppingListReadService readService,
        IEditingTracker editingTracker,
        AppDbContext dbContext,
        IMapper mapper)
    {
        _shoppingListService = shoppingListService;
        _readService = readService;
        _editingTracker = editingTracker;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    private Guid GetDeviceId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var deviceId))
            throw new HubException("Device ID not found in claims");
        return deviceId;
    }

    private async Task<DeviceInfo> GetDeviceInfoAsync(Guid deviceId)
    {
        var device = await _dbContext.Devices.FindAsync(deviceId);
        if (device == null) throw new HubException("Device not found");
        return _mapper.Map<DeviceInfo>(device);
    }

    private async Task BroadcastCurrentlyEditingAsync(Guid listId)
    {
        var editors = await _editingTracker.GetEditingDevicesAsync(listId);
        await Clients.Group($"list-{listId}").SendAsync("CurrentlyEditingChanged", new CurrentlyEditingChangedEvent
        {
            ListId = listId,
            EditingDevices = editors
        });
    }

    // --- Group management with presence ---
    public async Task JoinList(Guid listId)
    {
        var deviceId = GetDeviceId();

        // Verify access
        var summaries = await _readService.GetSummariesForDeviceAsync(deviceId, CancellationToken.None);
        if (!summaries.Any(s => s.Id == listId))
            throw new HubException("You don't have access to this list");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"list-{listId}");

        // Presence tracking
        var deviceInfo = await GetDeviceInfoAsync(deviceId);
        await _editingTracker.AddDeviceAsync(listId, deviceInfo, Context.ConnectionId, Context.ConnectionAborted);
        await BroadcastCurrentlyEditingAsync(listId);
    }

    public async Task LeaveList(Guid listId)
    {
        var deviceId = GetDeviceId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"list-{listId}");
        await _editingTracker.RemoveDeviceAsync(listId, deviceId, Context.ConnectionAborted);
        await BroadcastCurrentlyEditingAsync(listId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var affectedListIds = await _editingTracker.RemoveConnectionAsync(Context.ConnectionId);
        foreach (var listId in affectedListIds)
            await BroadcastCurrentlyEditingAsync(listId);

        await base.OnDisconnectedAsync(exception);
    }

    // --- List operations ---
    public async Task<ShoppingListDto> CreateList(string title)
    {
        var deviceId = GetDeviceId();
        var list = await _shoppingListService.CreateListAsync(deviceId, title);
        return _mapper.Map<ShoppingListDto>(list);
    }

    public async Task UpdateListTitle(Guid listId, string newTitle)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.UpdateListTitleAsync(listId, deviceId, newTitle);
    }

    public async Task DeleteList(Guid listId)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.DeleteListAsync(listId, deviceId);
    }

    public async Task<ShoppingListDto> CopyList(Guid sourceId)
    {
        var deviceId = GetDeviceId();
        var newList = await _shoppingListService.CopyListAsync(sourceId, deviceId);
        return _mapper.Map<ShoppingListDto>(newList);
    }

    public async Task AddEditor(Guid listId, Guid newEditorId)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.AddEditorAsync(listId, deviceId, newEditorId);
    }

    public async Task RemoveEditor(Guid listId, Guid editorId)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.RemoveEditorAsync(listId, deviceId, editorId);
    }

    // --- Category operations ---
    public async Task AddCategory(Guid listId, string categoryName)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.AddCategoryAsync(listId, deviceId, categoryName);
    }

    public async Task UpdateCategory(Guid categoryId, string newName)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.UpdateCategoryNameAsync(categoryId, deviceId, newName);
    }

    public async Task DeleteCategory(Guid categoryId)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.DeleteCategoryAsync(categoryId, deviceId);
    }

    public async Task ReorderCategory(Guid categoryId, int newPosition)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.ReorderCategoryAsync(categoryId, deviceId, newPosition);
    }

    // --- Item operations ---
    public async Task AddItem(Guid categoryId, string description)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.AddItemAsync(categoryId, deviceId, description);
    }

    public async Task UpdateItemDescription(Guid itemId, string newDescription)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.UpdateItemDescriptionAsync(itemId, deviceId, newDescription);
    }

    public async Task ToggleItem(Guid itemId, bool isChecked)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.ToggleItemCheckedAsync(itemId, deviceId, isChecked);
    }

    public async Task DeleteItem(Guid itemId)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.DeleteItemAsync(itemId, deviceId);
    }

    public async Task ReorderItem(Guid categoryId, Guid itemId, int newPosition)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.ReorderItemAsync(categoryId, deviceId, itemId, newPosition);
    }

    public async Task MoveItem(Guid itemId, Guid newCategoryId)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.MoveItemToCategoryAsync(itemId, deviceId, newCategoryId);
    }

    public async Task ResetCheckedItems(Guid listId)
    {
        var deviceId = GetDeviceId();
        await _shoppingListService.ResetCheckedItemsAsync(listId, deviceId);
    }
}
