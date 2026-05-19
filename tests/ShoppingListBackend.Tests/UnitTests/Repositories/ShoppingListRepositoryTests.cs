using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Repositories;

public class ShoppingListRepositoryTests : TestBase
{
    private readonly ShoppingListRepository _repository;

    public ShoppingListRepositoryTests()
    {
        _repository = new ShoppingListRepository(_context);
    }

    [Fact]
    public async Task Add_And_GetByIdAsync_IncludesAllRelations()
    {
        var owner = TestData.CreateDevice();
        var editor = TestData.CreateDevice();
        var list = TestData.CreateShoppingList(ownerId: owner.Id);
        list.Editors.Add(editor);
        var category = TestData.CreateCategory(list.Id, 0);
        var item = TestData.CreateItem(category.Id, 0);
        list.Categories.Add(category);
        category.Items.Add(item);
        _context.Devices.AddRange(owner, editor);
        _repository.Add(list);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(list.Id);
        retrieved.Should().NotBeNull();
        retrieved.Owner.Id.Should().Be(owner.Id);
        retrieved.Editors.Should().Contain(e => e.Id == editor.Id);
        retrieved.Categories.Should().ContainSingle(c => c.Id == category.Id);
        retrieved.Categories.First().Items.Should().ContainSingle(i => i.Id == item.Id);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_IncludesItems()
    {
        var list = TestData.CreateShoppingList(ownerId: Guid.NewGuid());
        var category = TestData.CreateCategory(list.Id, 0);
        var item = TestData.CreateItem(category.Id, 0);
        list.Categories.Add(category);
        category.Items.Add(item);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetCategoryByIdAsync(category.Id);
        retrieved.Should().NotBeNull();
        retrieved.Items.Should().ContainSingle(i => i.Id == item.Id);
    }

    [Fact]
    public async Task GetItemByIdAsync_ReturnsItem()
    {
        var list = TestData.CreateShoppingList(ownerId: Guid.NewGuid());
        var category = TestData.CreateCategory(list.Id, 0);
        var item = TestData.CreateItem(category.Id, 0);
        list.Categories.Add(category);
        category.Items.Add(item);
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetItemByIdAsync(item.Id);
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task Delete_List_Cascades()
    {
        var list = TestData.CreateShoppingList(ownerId: Guid.NewGuid());
        var category = TestData.CreateCategory(list.Id, 0);
        var item = TestData.CreateItem(category.Id, 0);
        list.Categories.Add(category);
        category.Items.Add(item);
        _repository.Add(list);
        await _context.SaveChangesAsync();

        _repository.Delete(list);
        await _context.SaveChangesAsync();

        var lists = await _context.ShoppingLists.ToListAsync();
        var categories = await _context.ShoppingListCategories.ToListAsync();
        var items = await _context.ShoppingListItems.ToListAsync();
        lists.Should().BeEmpty();
        categories.Should().BeEmpty();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddCategory_And_DeleteCategory_Work()
    {
        var list = TestData.CreateShoppingList(ownerId: Guid.NewGuid());
        _repository.Add(list);
        await _context.SaveChangesAsync();

        var category = TestData.CreateCategory(list.Id, 0);
        _repository.AddCategory(category);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetCategoryByIdAsync(category.Id);
        retrieved.Should().NotBeNull();

        _repository.DeleteCategory(category);
        await _context.SaveChangesAsync();

        var deleted = await _repository.GetCategoryByIdAsync(category.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task AddItem_And_DeleteItem_Work()
    {
        var list = TestData.CreateShoppingList(ownerId: Guid.NewGuid());
        var category = TestData.CreateCategory(list.Id, 0);
        _repository.Add(list);
        _repository.AddCategory(category);
        await _context.SaveChangesAsync();

        var item = TestData.CreateItem(category.Id, 0);
        _repository.AddItem(item);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetItemByIdAsync(item.Id);
        retrieved.Should().NotBeNull();

        _repository.DeleteItem(item);
        await _context.SaveChangesAsync();

        var deleted = await _repository.GetItemByIdAsync(item.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetCheckedItemsForListAsync_ReturnsOnlyCheckedItemsInList()
    {
        var list = TestData.CreateShoppingList(ownerId: Guid.NewGuid());
        var cat1 = TestData.CreateCategory(list.Id, 0);
        var cat2 = TestData.CreateCategory(list.Id, 1);
        var item1 = TestData.CreateItem(cat1.Id, 0, isChecked: true);
        var item2 = TestData.CreateItem(cat2.Id, 0);
        list.Categories.Add(cat1);
        list.Categories.Add(cat2);
        cat1.Items.Add(item1);
        cat2.Items.Add(item2);
        _repository.Add(list);
        await _context.SaveChangesAsync();

        var items = await _repository.GetCheckedItemsForListAsync(list.Id);
        items.Should().HaveCount(1);
        items.Should().Contain(i => i.Id == item1.Id);
        items.Should().NotContain(i => i.Id == item2.Id);
    }
}
