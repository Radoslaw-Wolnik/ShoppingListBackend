using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.Device;
using ShoppingListBackend.Api.DTOs.ShoppingList;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;
using ShoppingListBackend.Api.Mappers;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Tests.Helpers;
using System.Linq;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Mappers;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_ShouldBeValid()
    {
        // Arrange & Act
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        // Assert
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Device_To_DeviceInfo_MapsCorrectly()
    {
        // Arrange
        var device = TestData.CreateDevice();
        device.UserName = "TestUser";
        device.Colour = "#FF00AA";

        // Act
        var dto = _mapper.Map<DeviceInfo>(device);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(device.Id);
        dto.UserName.Should().Be(device.UserName);
        dto.Colour.Should().Be(device.Colour);
    }

    [Fact]
    public void Device_To_FriendDto_MapsCorrectly()
    {
        // Arrange
        var device = TestData.CreateDevice();
        device.UserName = "FriendUser";
        device.Colour = "#00FF00";

        // Act
        var dto = _mapper.Map<FriendDto>(device);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(device.Id);
        dto.UserName.Should().Be(device.UserName);
        dto.Colour.Should().Be(device.Colour);
    }

    [Fact]
    public void ShoppingListItem_To_ShoppingListItemDto_MapsCorrectly()
    {
        // Arrange
        var item = TestData.CreateItem(Guid.NewGuid(), 2, "Milk", true);
        var categoryId = item.ShoppingListCategoryId;

        // Act
        var dto = _mapper.Map<ShoppingListItemDto>(item);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(item.Id);
        dto.Description.Should().Be(item.Description);
        dto.IsChecked.Should().Be(item.IsChecked);
        dto.Position.Should().Be(item.Position);
        dto.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public void ShoppingListCategory_To_ShoppingListCategoryDto_MapsCorrectly()
    {
        // Arrange
        var category = TestData.CreateCategory(Guid.NewGuid(), 1, "Dairy");
        var item1 = TestData.CreateItem(category.Id, 0, "Milk", false);
        var item2 = TestData.CreateItem(category.Id, 1, "Cheese", true);
        category.Items.Add(item1);
        category.Items.Add(item2);

        // Act
        var dto = _mapper.Map<ShoppingListCategoryDto>(category);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(category.Id);
        dto.Name.Should().Be(category.Name);
        dto.Position.Should().Be(category.Position);
        dto.Items.Should().HaveCount(2);
        // Ensure items are ordered by Position
        dto.Items[0].Position.Should().Be(0);
        dto.Items[1].Position.Should().Be(1);
    }

    [Fact]
    public void ShoppingList_To_ShoppingListDto_MapsCorrectly()
    {
        // Arrange
        var owner = TestData.CreateDevice(username: "Owner", colour: "#FF0000");
        var editor1 = TestData.CreateDevice(username: "Editor1", colour: "#00FF00");
        var editor2 = TestData.CreateDevice(username: "Editor2", colour: "#0000FF");
        var list = TestData.CreateShoppingList(ownerId: owner.Id, title: "My List");
        list.Owner = owner;
        list.Editors.Add(editor1);
        list.Editors.Add(editor2);

        var category1 = TestData.CreateCategory(list.Id, 1, "Produce");
        var item1 = TestData.CreateItem(category1.Id, 0, "Apple");
        var item2 = TestData.CreateItem(category1.Id, 1, "Banana");
        category1.Items.Add(item1);
        category1.Items.Add(item2);

        var category2 = TestData.CreateCategory(list.Id, 0, "Dairy"); // position 0 should come before category1
        var item3 = TestData.CreateItem(category2.Id, 0, "Milk");
        category2.Items.Add(item3);

        list.Categories.Add(category1);
        list.Categories.Add(category2);

        // Act
        var dto = _mapper.Map<ShoppingListDto>(list);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(list.Id);
        dto.Title.Should().Be(list.Title);
        dto.UpdatedAt.Should().Be(list.UpdatedAt ?? list.CreatedAt);

        dto.Owner.Id.Should().Be(owner.Id);
        dto.Owner.UserName.Should().Be(owner.UserName);
        dto.Owner.Colour.Should().Be(owner.Colour);

        dto.Editors.Should().HaveCount(2);
        dto.Editors.Should().Contain(e => e.Id == editor1.Id && e.UserName == editor1.UserName);
        dto.Editors.Should().Contain(e => e.Id == editor2.Id && e.UserName == editor2.UserName);

        // Categories should be ordered by Position (category2 position 0, then category1 position 1)
        dto.Categories.Should().HaveCount(2);
        dto.Categories[0].Id.Should().Be(category2.Id);
        dto.Categories[0].Name.Should().Be("Dairy");
        dto.Categories[1].Id.Should().Be(category1.Id);
        dto.Categories[1].Name.Should().Be("Produce");

        // Items in each category should be ordered by Position
        dto.Categories[1].Items.Should().HaveCount(2);
        dto.Categories[1].Items[0].Description.Should().Be("Apple");
        dto.Categories[1].Items[1].Description.Should().Be("Banana");
    }

    [Fact]
    public void ShoppingList_To_DeviceShoppingListHeader_MapsCorrectly()
    {
        // Arrange
        var owner = TestData.CreateDevice(username: "Owner");
        var editor = TestData.CreateDevice(username: "Editor");
        var list = TestData.CreateShoppingList(ownerId: owner.Id, title: "Header List");
        list.Owner = owner;
        list.Editors.Add(editor);
        list.UpdatedAt = DateTime.UtcNow;

        // Act
        var dto = _mapper.Map<DeviceShoppingListHeader>(list);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(list.Id);
        dto.Title.Should().Be(list.Title);
        dto.UpdatedAt.Should().Be(list.UpdatedAt.Value);
        dto.Owner.Id.Should().Be(owner.Id);
        dto.Editors.Should().Contain(e => e.Id == editor.Id);
    }
}
