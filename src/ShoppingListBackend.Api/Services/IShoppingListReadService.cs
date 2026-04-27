using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.ShoppingList;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;

namespace ShoppingListBackend.Api.Services;

public interface IShoppingListReadService
{
    /// <summary>
    /// Returns lightweight headers of all lists that the device can access (owner or editor).
    /// </summary>
    Task<List<DeviceShoppingListHeader>> GetHeadersForDeviceAsync(
        Guid deviceId,
        DateTime? since = null,
        CancellationToken ct = default);

    /// <summary>
    /// Alias for GetHeadersForDeviceAsync – used by the hub.
    /// </summary>
    Task<List<DeviceShoppingListHeader>> GetSummariesForDeviceAsync(
        Guid deviceId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the full hydrated list (with categories and items) if the device has access.
    /// </summary>
    Task<ShoppingListDto> GetHydratedListAsync(
        Guid listId,
        Guid deviceId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the list of devices currently editing the given list (real‑time presence).
    /// </summary>
    Task<List<DeviceInfo>> GetCurrentlyEditingAsync(Guid listId);
}