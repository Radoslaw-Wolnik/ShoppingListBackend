using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Data;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.ShoppingList;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;
using ShoppingListBackend.Api.Exceptions;

namespace ShoppingListBackend.Api.Services;

public class ShoppingListReadService : IShoppingListReadService
{
    private readonly AppDbContext _context;
    private readonly IEditingTracker _editingTracker;

    public ShoppingListReadService(AppDbContext context, IEditingTracker editingTracker)
    {
        _context = context;
        _editingTracker = editingTracker;
    }

    public async Task<List<DeviceShoppingListHeader>> GetHeadersForDeviceAsync(
        Guid deviceId,
        DateTime? since = null,
        CancellationToken ct = default)
    {
        var query = _context.ShoppingLists
            .Where(sl => sl.OwnerDeviceId == deviceId || sl.Editors.Any(e => e.Id == deviceId))
            .Select(sl => new DeviceShoppingListHeader
            {
                Id = sl.Id,
                Title = sl.Title,
                UpdatedAt = sl.UpdatedAt ?? sl.CreatedAt,
                Owner = new DeviceInfo
                {
                    Id = sl.Owner.Id,
                    UserName = sl.Owner.UserName,
                    Colour = sl.Owner.Colour
                },
                Editors = sl.Editors.Select(e => new DeviceInfo
                {
                    Id = e.Id,
                    UserName = e.UserName,
                    Colour = e.Colour
                }).ToList()
            });

        if (since.HasValue)
            query = query.Where(h => h.UpdatedAt > since.Value);

        return await query.ToListAsync(ct);
    }

    // Explicit implementation of the alias – calls the same method
    public async Task<List<DeviceShoppingListHeader>> GetSummariesForDeviceAsync(
        Guid deviceId,
        CancellationToken ct = default)
    {
        return await GetHeadersForDeviceAsync(deviceId, null, ct);
    }

    public async Task<ShoppingListDto> GetHydratedListAsync(
        Guid listId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        var list = await _context.ShoppingLists
            .Where(sl => sl.Id == listId)
            .Where(sl => sl.OwnerDeviceId == deviceId || sl.Editors.Any(e => e.Id == deviceId))
            .Select(sl => new ShoppingListDto
            {
                Id = sl.Id,
                Title = sl.Title,
                UpdatedAt = sl.UpdatedAt ?? sl.CreatedAt,
                Owner = new DeviceInfo
                {
                    Id = sl.Owner.Id,
                    UserName = sl.Owner.UserName,
                    Colour = sl.Owner.Colour
                },
                Editors = sl.Editors.Select(e => new DeviceInfo
                {
                    Id = e.Id,
                    UserName = e.UserName,
                    Colour = e.Colour
                }).ToList(),
                Categories = sl.Categories.OrderBy(c => c.Position)
                    .Select(c => new ShoppingListCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Position = c.Position,
                        Items = c.Items.OrderBy(i => i.Position)
                            .Select(i => new ShoppingListItemDto
                            {
                                Id = i.Id,
                                Description = i.Description,
                                IsChecked = i.IsChecked,
                                Position = i.Position,
                                CategoryId = c.Id
                            }).ToList()
                    }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (list == null)
            throw new NotFoundException($"Shopping list {listId} not found or no access");

        return list;
    }

    public Task<List<DeviceInfo>> GetCurrentlyEditingAsync(Guid listId)
    {
        return _editingTracker.GetEditingDevicesAsync(listId);
    }
}
