using FluentAssertions;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public class DatabaseEditingTrackerTests : TestBase
{
    private readonly DatabaseEditingTracker _tracker;

    public DatabaseEditingTrackerTests()
    {
        _tracker = new DatabaseEditingTracker(_context);
    }

    [Fact]
    public async Task RemoveConnectionAsync_ReturnsAffectedLists_AndRemovesDevice()
    {
        var (listId, device) = await SeedListAndDeviceAsync();
        var deviceInfo = new DeviceInfo { Id = device.Id, UserName = device.UserName, Colour = device.Colour };

        await _tracker.AddDeviceAsync(listId, deviceInfo, "connection-1");
        var affectedLists = await _tracker.RemoveConnectionAsync("connection-1");

        affectedLists.Should().ContainSingle().Which.Should().Be(listId);
        var editingDevices = await _tracker.GetEditingDevicesAsync(listId);
        editingDevices.Should().BeEmpty();
    }

    [Fact]
    public async Task AddDeviceAsync_UpdatesExistingDeviceConnection()
    {
        var (listId, device) = await SeedListAndDeviceAsync();
        var deviceInfo = new DeviceInfo { Id = device.Id, UserName = device.UserName, Colour = device.Colour };

        await _tracker.AddDeviceAsync(listId, deviceInfo, "old-connection");
        await _tracker.AddDeviceAsync(listId, deviceInfo, "new-connection");
        var affectedLists = await _tracker.RemoveConnectionAsync("old-connection");

        affectedLists.Should().BeEmpty();
        var editingDevices = await _tracker.GetEditingDevicesAsync(listId);
        editingDevices.Should().ContainSingle(d => d.Id == device.Id);
    }

    [Fact]
    public async Task RemoveConnectionAsync_LeavesOtherConnectionsIntact()
    {
        var listId = Guid.NewGuid();
        var owner = TestData.CreateDevice();
        var removedDevice = TestData.CreateDevice();
        var remainingDevice = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(listId, owner.Id);
        _context.Devices.AddRange(owner, removedDevice, remainingDevice);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        await _tracker.AddDeviceAsync(listId, new DeviceInfo { Id = removedDevice.Id, UserName = removedDevice.UserName, Colour = removedDevice.Colour }, "connection-1");
        await _tracker.AddDeviceAsync(listId, new DeviceInfo { Id = remainingDevice.Id, UserName = remainingDevice.UserName, Colour = remainingDevice.Colour }, "connection-2");
        await _tracker.RemoveConnectionAsync("connection-1");

        var editingDevices = await _tracker.GetEditingDevicesAsync(listId);
        editingDevices.Should().ContainSingle(d => d.Id == remainingDevice.Id);
    }

    private async Task<(Guid ListId, ShoppingListBackend.Api.Models.Device Device)> SeedListAndDeviceAsync()
    {
        var owner = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(ownerId: owner.Id);
        _context.Devices.Add(owner);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();
        return (list.Id, owner);
    }
}
