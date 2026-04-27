using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.IntegrationTests.Database;

public class DbContextTests : TestBase
{
    [Fact]
    public async Task Can_Create_And_Retrieve_Device()
    {
        var device = TestData.CreateDevice();
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Devices.FindAsync(device.Id);
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(device.Id);
    }

    [Fact]
    public async Task Can_Create_ShoppingList_With_Owner()
    {
        var owner = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(ownerId: owner.Id);
        _context.Devices.Add(owner);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        var retrieved = await _context.ShoppingLists
            .Include(l => l.Owner)
            .FirstOrDefaultAsync(l => l.Id == list.Id);
        retrieved.Should().NotBeNull();
        retrieved.Owner.Id.Should().Be(owner.Id);
    }

    [Fact]
    public async Task Can_Add_Editor_To_ShoppingList()
    {
        var owner = TestData.CreateDevice();
        var editor = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(ownerId: owner.Id);
        list.Editors.Add(editor);
        _context.Devices.AddRange(owner, editor);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        var retrieved = await _context.ShoppingLists
            .Include(l => l.Editors)
            .FirstOrDefaultAsync(l => l.Id == list.Id);
        retrieved!.Editors.Should().Contain(e => e.Id == editor.Id);
    }

    [Fact]
    public async Task Deleting_List_Cascades_To_Categories_And_Items()
    {
        var owner = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(ownerId: owner.Id);
        var category = TestData.CreateCategory(list.Id, 0);
        var item = TestData.CreateItem(category.Id, 0);
        list.Categories.Add(category);
        category.Items.Add(item);
        _context.Devices.Add(owner);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        _context.ShoppingLists.Remove(list);
        await _context.SaveChangesAsync();

        var categories = await _context.ShoppingListCategories.ToListAsync();
        var items = await _context.ShoppingListItems.ToListAsync();
        categories.Should().BeEmpty();
        items.Should().BeEmpty();
    }
}