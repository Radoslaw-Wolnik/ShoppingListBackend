using FluentAssertions;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.Services;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public class InMemoryEditingTrackerTests
{
    [Fact]
    public void RemoveConnection_ReturnsAffectedLists_AndRemovesDevice()
    {
        var tracker = new InMemoryEditingTracker();
        var listId = Guid.NewGuid();
        var device = new DeviceInfo { Id = Guid.NewGuid(), UserName = "Test", Colour = "#FFFFFF" };

        tracker.AddDevice(listId, device, "connection-1");
        var affectedLists = tracker.RemoveConnection("connection-1");

        affectedLists.Should().ContainSingle().Which.Should().Be(listId);
        tracker.GetEditingDevices(listId).Should().BeEmpty();
    }

    [Fact]
    public void RemoveConnection_LeavesOtherConnectionsIntact()
    {
        var tracker = new InMemoryEditingTracker();
        var listId = Guid.NewGuid();
        var removedDevice = new DeviceInfo { Id = Guid.NewGuid(), UserName = "A", Colour = "#FFFFFF" };
        var remainingDevice = new DeviceInfo { Id = Guid.NewGuid(), UserName = "B", Colour = "#000000" };

        tracker.AddDevice(listId, removedDevice, "connection-1");
        tracker.AddDevice(listId, remainingDevice, "connection-2");
        tracker.RemoveConnection("connection-1");

        tracker.GetEditingDevices(listId).Should().ContainSingle(d => d.Id == remainingDevice.Id);
    }
}
