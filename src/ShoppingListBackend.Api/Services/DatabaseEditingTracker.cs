using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Data;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.Models;

namespace ShoppingListBackend.Api.Services;

/// <summary>
/// Database-backed presence tracker. Keeping presence in PostgreSQL means each API
/// instance sees the same currently-editing state when the app is horizontally scaled.
/// </summary>
public class DatabaseEditingTracker(AppDbContext context) : IEditingTracker
{
    private readonly AppDbContext _context = context;

    public async Task AddDeviceAsync(Guid listId, DeviceInfo device, string connectionId, CancellationToken ct = default)
    {
        // A device has at most one active presence row per list. Rejoining from a new
        // connection updates the row so an old disconnect cannot remove fresh presence.
        var session = await _context.EditingSessions.FindAsync([listId, device.Id], ct);
        if (session is null)
        {
            _context.EditingSessions.Add(new EditingSession
            {
                ListId = listId,
                DeviceId = device.Id,
                ConnectionId = connectionId,
                UserName = device.UserName,
                Colour = device.Colour,
                LastSeenAt = DateTime.UtcNow
            });
        }
        else
        {
            session.ConnectionId = connectionId;
            session.UserName = device.UserName;
            session.Colour = device.Colour;
            session.LastSeenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveDeviceAsync(Guid listId, Guid deviceId, CancellationToken ct = default)
    {
        var session = await _context.EditingSessions.FindAsync([listId, deviceId], ct);
        if (session is null)
            return;

        _context.EditingSessions.Remove(session);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<Guid>> RemoveConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        var sessions = await _context.EditingSessions
            .Where(session => session.ConnectionId == connectionId)
            .ToListAsync(ct);

        if (sessions.Count == 0)
            return [];

        var affectedListIds = sessions.Select(session => session.ListId).Distinct().ToList();
        _context.EditingSessions.RemoveRange(sessions);
        await _context.SaveChangesAsync(ct);
        return affectedListIds;
    }

    public Task<List<DeviceInfo>> GetEditingDevicesAsync(Guid listId, CancellationToken ct = default)
    {
        return _context.EditingSessions
            .AsNoTracking()
            .Where(session => session.ListId == listId)
            .OrderBy(session => session.UserName)
            .Select(session => new DeviceInfo
            {
                Id = session.DeviceId,
                UserName = session.UserName,
                Colour = session.Colour
            })
            .ToListAsync(ct);
    }
}
