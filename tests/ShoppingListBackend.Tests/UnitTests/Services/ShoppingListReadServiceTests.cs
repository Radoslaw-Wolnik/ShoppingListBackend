using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.Exceptions;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public class ShoppingListReadServiceTests : TestBase
{
    private readonly InMemoryEditingTracker _tracker;
    private readonly ShoppingListReadService _readService;

    public ShoppingListReadServiceTests()
    {
        _tracker = new InMemoryEditingTracker();
        _readService = new ShoppingListReadService(_context, _tracker);
    }

    [Fact]
    public async Task GetHeadersForDeviceAsync_ReturnsOnlyAccessibleLists()
    {
        var owner = TestData.CreateDevice();
        var editor = TestData.CreateDevice();
        var listOwned = TestData.CreateShoppingList(ownerId: owner.Id, title: "Owner List");
        var listEditable = TestData.CreateShoppingList(ownerId: Guid.NewGuid(), title: "Editable List");
        listEditable.Editors.Add(editor);
        var otherList = TestData.CreateShoppingList(ownerId: Guid.NewGuid(), title: "Other");

        _context.Devices.AddRange(owner, editor);
        _context.ShoppingLists.AddRange(listOwned, listEditable, otherList);
        await _context.SaveChangesAsync();

        var headersForOwner = await _readService.GetHeadersForDeviceAsync(owner.Id);
        headersForOwner.Select(h => h.Title).Should().Contain("Owner List").And.NotContain("Editable List", "Other");

        var headersForEditor = await _readService.GetHeadersForDeviceAsync(editor.Id);
        headersForEditor.Select(h => h.Title).Should().Contain("Editable List").And.NotContain("Owner List", "Other");
    }

    [Fact]
    public async Task GetHeadersForDeviceAsync_FiltersBySince()
    {
        var owner = TestData.CreateDevice();
        var list1 = TestData.CreateShoppingList(ownerId: owner.Id);
        list1.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        var list2 = TestData.CreateShoppingList(ownerId: owner.Id);
        list2.UpdatedAt = DateTime.UtcNow;
        _context.Devices.Add(owner);
        _context.ShoppingLists.AddRange(list1, list2);
        await _context.SaveChangesAsync();

        var since = DateTime.UtcNow.AddMinutes(-5);
        var headers = await _readService.GetHeadersForDeviceAsync(owner.Id, since);
        headers.Should().ContainSingle(h => h.Id == list2.Id);
        headers.Should().NotContain(h => h.Id == list1.Id);
    }

    [Fact]
    public async Task GetHydratedListAsync_ReturnsFullList_WhenOwner()
    {
        var owner = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(ownerId: owner.Id);
        var cat1 = TestData.CreateCategory(list.Id, 1);
        var cat2 = TestData.CreateCategory(list.Id, 0);
        var item1 = TestData.CreateItem(cat1.Id, 0);
        var item2 = TestData.CreateItem(cat2.Id, 0);
        list.Categories.Add(cat1);
        list.Categories.Add(cat2);
        cat1.Items.Add(item1);
        cat2.Items.Add(item2);
        _context.Devices.Add(owner);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        var dto = await _readService.GetHydratedListAsync(list.Id, owner.Id);
        dto.Id.Should().Be(list.Id);
        dto.Categories.Count.Should().Be(2);
        // categories ordered by Position (cat2 position 0, cat1 position 1)
        dto.Categories[0].Id.Should().Be(cat2.Id);
        dto.Categories[1].Id.Should().Be(cat1.Id);
        dto.Categories[0].Items.Should().ContainSingle(i => i.Id == item2.Id);
        dto.Categories[1].Items.Should().ContainSingle(i => i.Id == item1.Id);
    }

    [Fact]
    public async Task GetHydratedListAsync_ThrowsNotFoundException_WhenNoAccess()
    {
        var owner = TestData.CreateDevice();
        var other = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(ownerId: owner.Id);
        _context.Devices.AddRange(owner, other);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        Func<Task> act = () => _readService.GetHydratedListAsync(list.Id, other.Id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCurrentlyEditingAsync_ReturnsDevicesFromTracker()
    {
        var listId = Guid.NewGuid();
        var device1 = new DeviceInfo { Id = Guid.NewGuid(), UserName = "A", Colour = "#111" };
        var device2 = new DeviceInfo { Id = Guid.NewGuid(), UserName = "B", Colour = "#222" };
        _tracker.AddDevice(listId, device1, "conn1");
        _tracker.AddDevice(listId, device2, "conn2");

        var editing = await _readService.GetCurrentlyEditingAsync(listId);
        editing.Should().Contain(d => d.Id == device1.Id);
        editing.Should().Contain(d => d.Id == device2.Id);
    }
}