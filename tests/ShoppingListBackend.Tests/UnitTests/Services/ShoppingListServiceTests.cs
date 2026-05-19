using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingListBackend.Api.DTOs.RealTime;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public class ShoppingListServiceTests : ShoppingListServiceTestsBase
{
    // Helper to create a full test list
    private (ShoppingList list, List<ShoppingListCategory> categories, List<ShoppingListItem> items) CreateTestList(Guid listId, Guid ownerId)
    {
        var list = TestData.CreateShoppingList(listId, ownerId);
        var categories = new List<ShoppingListCategory>();
        var items = new List<ShoppingListItem>();

        for (int i = 0; i < 2; i++)
        {
            var category = TestData.CreateCategory(listId, i);
            categories.Add(category);
            for (int j = 0; j < 2; j++)
            {
                var item = TestData.CreateItem(category.Id, j);
                items.Add(item);
                category.Items.Add(item);
            }
            list.Categories.Add(category);
        }
        return (list, categories, items);
    }

    [Fact]
    public async Task CreateListAsync_ShouldCreateListAndReturn()
    {
        var ownerId = Guid.NewGuid();
        var title = "My List";
        ShoppingList? capturedList = null;
        _repoMock.Setup(r => r.Add(It.IsAny<ShoppingList>())).Callback<ShoppingList>(l =>
        {
            l.Id = Guid.NewGuid();
            l.CreatedAt = DateTime.UtcNow;
            capturedList = l;
        });

        var result = await _service.CreateListAsync(ownerId, title);

        result.Should().NotBeNull();
        result.Title.Should().Be(title);
        result.OwnerDeviceId.Should().Be(ownerId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _repoMock.Verify(r => r.Add(It.Is<ShoppingList>(l => l.Title == title && l.OwnerDeviceId == ownerId)), Times.Once);
        capturedList.Should().NotBeNull();
        capturedList.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateListTitleAsync_ShouldUpdate_WhenOwner()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        SetupGetListWithCategoriesAndItems(listId, list);
        var newTitle = "Updated Title";

        await _service.UpdateListTitleAsync(listId, ownerId, newTitle);

        list.Title.Should().Be(newTitle);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<ListUpdatedEvent>($"list-{listId}", e => e.Title == newTitle);
    }

    [Fact]
    public async Task UpdateListTitleAsync_ShouldThrow_WhenNotOwner()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        SetupGetListWithCategoriesAndItems(listId, list);

        Func<Task> act = () => _service.UpdateListTitleAsync(listId, requesterId, "New");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteListAsync_ShouldDelete_WhenOwner()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        SetupGetListWithCategoriesAndItems(listId, list);

        await _service.DeleteListAsync(listId, ownerId);

        _repoMock.Verify(r => r.Delete(list), Times.Once);
        VerifyBroadcast<ListDeletedEvent>($"list-{listId}", e => true);
    }

    [Fact]
    public async Task DeleteListAsync_ShouldThrow_WhenNotOwner()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        SetupGetListWithCategoriesAndItems(listId, list);

        Func<Task> act = () => _service.DeleteListAsync(listId, requesterId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CopyListAsync_ShouldCreateCopy_WhenOwner()
    {
        var sourceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var (sourceList, categories, items) = CreateTestList(sourceId, ownerId);
        _repoMock.Setup(r => r.GetByIdAsync(sourceId, default)).ReturnsAsync(sourceList);
        _repoMock.Setup(r => r.Add(It.IsAny<ShoppingList>())).Callback<ShoppingList>(l =>
        {
            l.Id = Guid.NewGuid();
            l.CreatedAt = DateTime.UtcNow;
        });
        _repoMock.Setup(r => r.AddCategory(It.IsAny<ShoppingListCategory>())).Callback<ShoppingListCategory>(c => c.Id = Guid.NewGuid());
        _repoMock.Setup(r => r.AddItem(It.IsAny<ShoppingListItem>())).Callback<ShoppingListItem>(i => i.Id = Guid.NewGuid());

        var result = await _service.CopyListAsync(sourceId, ownerId);

        result.Should().NotBeNull();
        result.Title.Should().Be($"{sourceList.Title} (copy)");
        result.OwnerDeviceId.Should().Be(ownerId);
        _repoMock.Verify(r => r.Add(It.Is<ShoppingList>(l => l.Title == $"{sourceList.Title} (copy)" && l.OwnerDeviceId == ownerId)), Times.Once);
        _repoMock.Verify(r => r.AddCategory(It.IsAny<ShoppingListCategory>()), Times.Exactly(categories.Count));
        _repoMock.Verify(r => r.AddItem(It.IsAny<ShoppingListItem>()), Times.Exactly(items.Count));
    }

    [Fact]
    public async Task CopyListAsync_ShouldThrow_WhenNoPermission()
    {
        var sourceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var (sourceList, _, _) = CreateTestList(sourceId, ownerId);
        _repoMock.Setup(r => r.GetByIdAsync(sourceId, default)).ReturnsAsync(sourceList);

        Func<Task> act = () => _service.CopyListAsync(sourceId, requesterId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AddEditorAsync_ShouldAdd_WhenOwner()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var newEditorId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(newEditorId);
        SetupGetListWithCategoriesAndItems(listId, list);
        _context.Devices.Add(editor);
        await _context.SaveChangesAsync();

        await _service.AddEditorAsync(listId, ownerId, newEditorId);

        list.Editors.Should().Contain(e => e.Id == newEditorId);
        VerifyBroadcast<EditorsUpdatedEvent>($"list-{listId}", e => e.Editors.Any(d => d.Id == newEditorId));
    }

    [Fact]
    public async Task AddEditorAsync_ShouldThrow_WhenNotOwner()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        SetupGetListWithCategoriesAndItems(listId, list);

        Func<Task> act = () => _service.AddEditorAsync(listId, requesterId, Guid.NewGuid());
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RemoveEditorAsync_ShouldRemove_WhenOwner()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);

        await _service.RemoveEditorAsync(listId, ownerId, editorId);

        list.Editors.Should().NotContain(e => e.Id == editorId);
        VerifyBroadcast<EditorsUpdatedEvent>($"list-{listId}", e => !e.Editors.Any(d => d.Id == editorId));
    }

    [Fact]
    public async Task AddCategoryAsync_ShouldAdd_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, _, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        _repoMock.Setup(r => r.AddCategory(It.IsAny<ShoppingListCategory>())).Callback<ShoppingListCategory>(c => c.Id = Guid.NewGuid());

        var categoryName = "New Category";
        await _service.AddCategoryAsync(listId, editorId, categoryName);

        _repoMock.Verify(r => r.AddCategory(It.Is<ShoppingListCategory>(c =>
            c.Name == categoryName && c.ShoppingListId == listId && c.Position == list.Categories.Count - 1)), Times.Once);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<CategoryAddedEvent>($"list-{listId}", e => e.Category.Name == categoryName);
    }

    [Fact]
    public async Task UpdateCategoryNameAsync_ShouldUpdate_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var category = categories.First();
        _repoMock.Setup(r => r.GetCategoryByIdAsync(category.Id, default)).ReturnsAsync(category);

        var newName = "Updated Category";
        await _service.UpdateCategoryNameAsync(category.Id, editorId, newName);

        category.Name.Should().Be(newName);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<CategoryUpdatedEvent>($"list-{listId}", e => e.CategoryId == category.Id && e.Name == newName);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldDelete_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var category = categories.First();
        _repoMock.Setup(r => r.GetCategoryByIdAsync(category.Id, default)).ReturnsAsync(category);

        await _service.DeleteCategoryAsync(category.Id, editorId);

        _repoMock.Verify(r => r.DeleteCategory(category), Times.Once);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<CategoryDeletedEvent>($"list-{listId}", e => e.CategoryId == category.Id);
    }

    [Fact]
    public async Task ReorderCategoryAsync_ShouldReorder_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var categoryToMove = categories[0];
        _repoMock.Setup(r => r.GetCategoryByIdAsync(categoryToMove.Id, default)).ReturnsAsync(categoryToMove);

        await _service.ReorderCategoryAsync(categoryToMove.Id, editorId, 1);

        var ordered = list.Categories.OrderBy(c => c.Position).ToList();
        ordered[1].Should().Be(categoryToMove);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<CategoryReorderedEvent>($"list-{listId}", e => e.Categories.Count == categories.Count);
    }

    [Fact]
    public async Task AddItemAsync_ShouldAdd_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var category = categories.First();
        _repoMock.Setup(r => r.GetCategoryByIdAsync(category.Id, default)).ReturnsAsync(category);
        _repoMock.Setup(r => r.AddItem(It.IsAny<ShoppingListItem>())).Callback<ShoppingListItem>(i => i.Id = Guid.NewGuid());

        var description = "New item";
        await _service.AddItemAsync(category.Id, editorId, description);

        _repoMock.Verify(r => r.AddItem(It.Is<ShoppingListItem>(i =>
            i.Description == description && i.ShoppingListCategoryId == category.Id && i.Position == category.Items.Count - 1)), Times.Once);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<ItemAddedEvent>($"list-{listId}", e => e.Item.Description == description);
    }

    [Fact]
    public async Task UpdateItemDescriptionAsync_ShouldUpdate_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, items) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var item = items.First();
        var category = categories.First(c => c.Id == item.ShoppingListCategoryId);
        _repoMock.Setup(r => r.GetItemByIdAsync(item.Id, default)).ReturnsAsync(item);
        _repoMock.Setup(r => r.GetCategoryByIdAsync(category.Id, default)).ReturnsAsync(category);

        var newDescription = "Updated item";
        await _service.UpdateItemDescriptionAsync(item.Id, editorId, newDescription);

        item.Description.Should().Be(newDescription);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<ItemUpdatedEvent>($"list-{listId}", e => e.ItemId == item.Id && e.Description == newDescription && e.IsChecked == item.IsChecked);
    }

    [Fact]
    public async Task ToggleItemCheckedAsync_ShouldToggle_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, items) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var item = items.First();
        var category = categories.First(c => c.Id == item.ShoppingListCategoryId);
        _repoMock.Setup(r => r.GetItemByIdAsync(item.Id, default)).ReturnsAsync(item);
        _repoMock.Setup(r => r.GetCategoryByIdAsync(category.Id, default)).ReturnsAsync(category);

        var newCheckedState = !item.IsChecked;
        await _service.ToggleItemCheckedAsync(item.Id, editorId, newCheckedState);

        item.IsChecked.Should().Be(newCheckedState);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<ItemToggledEvent>($"list-{listId}", e => e.ItemId == item.Id && e.IsChecked == newCheckedState);
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldDelete_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, items) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var item = items.First();
        var category = categories.First(c => c.Id == item.ShoppingListCategoryId);
        _repoMock.Setup(r => r.GetItemByIdAsync(item.Id, default)).ReturnsAsync(item);
        _repoMock.Setup(r => r.GetCategoryByIdAsync(category.Id, default)).ReturnsAsync(category);

        await _service.DeleteItemAsync(item.Id, editorId);

        _repoMock.Verify(r => r.DeleteItem(item), Times.Once);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<ItemDeletedEvent>($"list-{listId}", e => e.ItemId == item.Id);
    }

    [Fact]
    public async Task ReorderItemAsync_ShouldReorder_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var category = categories.First();
        var item = category.Items.First();
        _repoMock.Setup(r => r.GetCategoryByIdAsync(category.Id, default)).ReturnsAsync(category);
        _repoMock.Setup(r => r.GetItemByIdAsync(item.Id, default)).ReturnsAsync(item);

        int newPosition = category.Items.Count - 1;
        await _service.ReorderItemAsync(category.Id, editorId, item.Id, newPosition);

        var itemsAfter = category.Items.OrderBy(i => i.Position).ToList();
        itemsAfter.Last().Should().Be(item);
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<ItemReorderedEvent>($"list-{listId}", e => e.CategoryId == category.Id && e.Items.Count == category.Items.Count);
    }

    [Fact]
    public async Task MoveItemToCategoryAsync_ShouldMove_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, categories, _) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var sourceCategory = categories[0];
        var destCategory = categories[1];
        var item = sourceCategory.Items.First();
        _repoMock.Setup(r => r.GetItemByIdAsync(item.Id, default)).ReturnsAsync(item);
        _repoMock.Setup(r => r.GetCategoryByIdAsync(sourceCategory.Id, default)).ReturnsAsync(sourceCategory);
        _repoMock.Setup(r => r.GetCategoryByIdAsync(destCategory.Id, default)).ReturnsAsync(destCategory);

        await _service.MoveItemToCategoryAsync(item.Id, editorId, destCategory.Id);

        item.ShoppingListCategoryId.Should().Be(destCategory.Id);
        sourceCategory.Items.Should().NotContain(item);
        destCategory.Items.Should().Contain(item);
        list.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetCheckedItemsAsync_ShouldReset_WhenEditor()
    {
        var listId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var (list, _, items) = CreateTestList(listId, ownerId);
        var editor = TestData.CreateDevice(editorId);
        list.Editors.Add(editor);
        SetupGetListWithCategoriesAndItems(listId, list);
        var checkedItem1 = items[0];
        var checkedItem2 = items[1];
        checkedItem1.IsChecked = true;
        checkedItem2.IsChecked = true;
        var checkedItems = new List<ShoppingListItem> { checkedItem1, checkedItem2 };
        _repoMock.Setup(r => r.GetCheckedItemsForListAsync(listId, default)).ReturnsAsync(checkedItems);

        await _service.ResetCheckedItemsAsync(listId, editorId);

        checkedItem1.IsChecked.Should().BeFalse();
        checkedItem2.IsChecked.Should().BeFalse();
        list.UpdatedAt.Should().NotBeNull();
        VerifyBroadcast<ListItemsResetEvent>($"list-{listId}", e => true);
    }
}
