using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingListBackend.Api.Data;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;
using ShoppingListBackend.Api.Hubs;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Hubs;

public class ShoppingListHubTests
{
    private readonly Mock<IShoppingListService> _serviceMock;
    private readonly Mock<IShoppingListReadService> _readServiceMock;
    private readonly Mock<IEditingTracker> _trackerMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<AppDbContext> _dbContextMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IGroupManager> _groupsMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly ShoppingListHub _hub;
    private readonly Guid _deviceId = Guid.NewGuid();

    public ShoppingListHubTests()
    {
        _serviceMock = new Mock<IShoppingListService>();
        _readServiceMock = new Mock<IShoppingListReadService>();
        _trackerMock = new Mock<IEditingTracker>();
        _mapperMock = new Mock<IMapper>();
        _dbContextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _clientsMock = new Mock<IHubCallerClients>();
        _clientProxyMock = new Mock<IClientProxy>();           // For Group and All
        var singleClientProxyMock = new Mock<ISingleClientProxy>(); // For Caller
        _groupsMock = new Mock<IGroupManager>();
        _contextMock = new Mock<HubCallerContext>();

        // Setup hub clients
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.Caller).Returns(singleClientProxyMock.Object);

        _hub = new ShoppingListHub(
            _serviceMock.Object,
            _readServiceMock.Object,
            _trackerMock.Object,
            _dbContextMock.Object,
            _mapperMock.Object)
        {
            Clients = _clientsMock.Object,
            Groups = _groupsMock.Object,
            Context = _contextMock.Object
        };

        // Setup authentication claim
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _deviceId.ToString()) };
        _contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims)));
    }

    [Fact]
    public async Task JoinList_AddsToTrackerAndBroadcastsPresence()
    {
        var listId = Guid.NewGuid();
        var summaries = new[] { new DeviceShoppingListHeader { Id = listId } };
        _readServiceMock.Setup(r => r.GetSummariesForDeviceAsync(_deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries.ToList());
        
        var deviceInfo = new DeviceInfo { Id = _deviceId, UserName = "Test", Colour = "#FFF" };
        var device = new Device { Id = _deviceId, UserName = "Test", Colour = "#FFF" };
        _dbContextMock.Setup(db => db.Devices.FindAsync(_deviceId))
            .ReturnsAsync(device);
        _mapperMock.Setup(m => m.Map<DeviceInfo>(device)).Returns(deviceInfo);

        await _hub.JoinList(listId);

        _trackerMock.Verify(t => t.AddDevice(listId, deviceInfo, _hub.Context.ConnectionId), Times.Once);
        _clientsMock.Verify(c => c.Group($"list-{listId}").SendAsync("CurrentlyEditingChanged", It.IsAny<List<DeviceInfo>>(), default), Times.Once);
    }

    [Fact]
    public async Task JoinList_Should_AddToGroup_When_AccessGranted()
    {
        var listId = Guid.NewGuid();
        var summaries = new[] { new DeviceShoppingListHeader { Id = listId } };
        _readServiceMock.Setup(r => r.GetSummariesForDeviceAsync(_deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries.ToList());

        await _hub.JoinList(listId);

        _groupsMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), $"list-{listId}", default), Times.Once);
    }

    [Fact]
    public async Task JoinList_Should_Throw_When_NoAccess()
    {
        var listId = Guid.NewGuid();
        var summaries = new[] { new DeviceShoppingListHeader { Id = Guid.NewGuid() } };
        _readServiceMock.Setup(r => r.GetSummariesForDeviceAsync(_deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries.ToList());

        Func<Task> act = () => _hub.JoinList(listId);
        await act.Should().ThrowAsync<HubException>().WithMessage("*access*");
        _groupsMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task LeaveList_Should_RemoveFromGroup()
    {
        var listId = Guid.NewGuid();
        await _hub.LeaveList(listId);
        _groupsMock.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), $"list-{listId}", default), Times.Once);
    }

    [Fact]
    public async Task CreateList_Should_CallService_And_ReturnDto()
    {
        var title = "New List";
        var expectedList = TestData.CreateShoppingList(title: title);
        _serviceMock.Setup(s => s.CreateListAsync(_deviceId, title)).ReturnsAsync(expectedList);
        _mapperMock.Setup(m => m.Map<ShoppingListDto>(expectedList)).Returns(new ShoppingListDto { Id = expectedList.Id, Title = expectedList.Title });

        var result = await _hub.CreateList(title);
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedList.Id);
        result.Title.Should().Be(title);
        _serviceMock.Verify(s => s.CreateListAsync(_deviceId, title), Times.Once);
    }

    [Fact]
    public async Task UpdateListTitle_Should_CallService()
    {
        var listId = Guid.NewGuid();
        var newTitle = "New Title";
        await _hub.UpdateListTitle(listId, newTitle);
        _serviceMock.Verify(s => s.UpdateListTitleAsync(listId, _deviceId, newTitle), Times.Once);
    }

    [Fact]
    public async Task DeleteList_Should_CallService()
    {
        var listId = Guid.NewGuid();
        await _hub.DeleteList(listId);
        _serviceMock.Verify(s => s.DeleteListAsync(listId, _deviceId), Times.Once);
    }

    [Fact]
    public async Task CopyList_Should_CallService_And_ReturnDto()
    {
        var sourceId = Guid.NewGuid();
        var expectedList = TestData.CreateShoppingList(title: "Copy");
        _serviceMock.Setup(s => s.CopyListAsync(sourceId, _deviceId)).ReturnsAsync(expectedList);
        _mapperMock.Setup(m => m.Map<ShoppingListDto>(expectedList)).Returns(new ShoppingListDto { Id = expectedList.Id });

        var result = await _hub.CopyList(sourceId);
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedList.Id);
        _serviceMock.Verify(s => s.CopyListAsync(sourceId, _deviceId), Times.Once);
    }

    [Fact]
    public async Task AddEditor_Should_CallService()
    {
        var listId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        await _hub.AddEditor(listId, editorId);
        _serviceMock.Verify(s => s.AddEditorAsync(listId, _deviceId, editorId), Times.Once);
    }

    [Fact]
    public async Task RemoveEditor_Should_CallService()
    {
        var listId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        await _hub.RemoveEditor(listId, editorId);
        _serviceMock.Verify(s => s.RemoveEditorAsync(listId, _deviceId, editorId), Times.Once);
    }

    [Fact]
    public async Task AddCategory_Should_CallService()
    {
        var listId = Guid.NewGuid();
        var categoryName = "Produce";
        await _hub.AddCategory(listId, categoryName);
        _serviceMock.Verify(s => s.AddCategoryAsync(listId, _deviceId, categoryName), Times.Once);
    }

    [Fact]
    public async Task UpdateCategory_Should_CallService()
    {
        var categoryId = Guid.NewGuid();
        var newName = "Updated";
        await _hub.UpdateCategory(categoryId, newName);
        _serviceMock.Verify(s => s.UpdateCategoryNameAsync(categoryId, _deviceId, newName), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_Should_CallService()
    {
        var categoryId = Guid.NewGuid();
        await _hub.DeleteCategory(categoryId);
        _serviceMock.Verify(s => s.DeleteCategoryAsync(categoryId, _deviceId), Times.Once);
    }

    [Fact]
    public async Task ReorderCategory_Should_CallService()
    {
        var categoryId = Guid.NewGuid();
        var newPosition = 2;
        await _hub.ReorderCategory(categoryId, newPosition);
        _serviceMock.Verify(s => s.ReorderCategoryAsync(categoryId, _deviceId, newPosition), Times.Once);
    }

    [Fact]
    public async Task AddItem_Should_CallService()
    {
        var categoryId = Guid.NewGuid();
        var description = "Apple";
        await _hub.AddItem(categoryId, description);
        _serviceMock.Verify(s => s.AddItemAsync(categoryId, _deviceId, description), Times.Once);
    }

    [Fact]
    public async Task UpdateItemDescription_Should_CallService()
    {
        var itemId = Guid.NewGuid();
        var newDescription = "Almond milk";
        await _hub.UpdateItemDescription(itemId, newDescription);
        _serviceMock.Verify(s => s.UpdateItemDescriptionAsync(itemId, _deviceId, newDescription), Times.Once);
    }

    [Fact]
    public async Task ToggleItem_Should_CallService()
    {
        var itemId = Guid.NewGuid();
        var isChecked = true;
        await _hub.ToggleItem(itemId, isChecked);
        _serviceMock.Verify(s => s.ToggleItemCheckedAsync(itemId, _deviceId, isChecked), Times.Once);
    }

    [Fact]
    public async Task DeleteItem_Should_CallService()
    {
        var itemId = Guid.NewGuid();
        await _hub.DeleteItem(itemId);
        _serviceMock.Verify(s => s.DeleteItemAsync(itemId, _deviceId), Times.Once);
    }

    [Fact]
    public async Task ReorderItem_Should_CallService()
    {
        var categoryId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var newPosition = 3;
        await _hub.ReorderItem(categoryId, itemId, newPosition);
        _serviceMock.Verify(s => s.ReorderItemAsync(categoryId, _deviceId, itemId, newPosition), Times.Once);
    }

    [Fact]
    public async Task MoveItem_Should_CallService()
    {
        var itemId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        await _hub.MoveItem(itemId, newCategoryId);
        _serviceMock.Verify(s => s.MoveItemToCategoryAsync(itemId, _deviceId, newCategoryId), Times.Once);
    }

    [Fact]
    public async Task ResetCheckedItems_Should_CallService()
    {
        var listId = Guid.NewGuid();
        await _hub.ResetCheckedItems(listId);
        _serviceMock.Verify(s => s.ResetCheckedItemsAsync(listId, _deviceId), Times.Once);
    }

    [Fact]
    public async Task Hub_Should_Throw_When_DeviceId_Not_In_Claims()
    {
        _contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal());
        var listId = Guid.NewGuid();
        Func<Task> act = () => _hub.JoinList(listId);
        await act.Should().ThrowAsync<HubException>().WithMessage("*Device ID*");
    }
}