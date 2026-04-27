using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.RealTime;
using ShoppingListBackend.Api.Hubs;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Tests.Helpers;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public abstract class ShoppingListServiceTestsBase : TestBase
{
    protected readonly Mock<IShoppingListRepository> _repoMock;
    protected readonly Mock<IHubContext<ShoppingListHub>> _hubContextMock;
    protected readonly Mock<IMapper> _mapperMock;
    protected readonly ShoppingListService _service;

    protected ShoppingListServiceTestsBase()
    {
        _repoMock = new Mock<IShoppingListRepository>();
        _hubContextMock = new Mock<IHubContext<ShoppingListHub>>();
        _mapperMock = new Mock<IMapper>();

        _service = new ShoppingListService(
            _repoMock.Object,
            _context,
            _hubContextMock.Object,
            _mapperMock.Object
        );

        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
    }

    protected void SetupGetListWithCategoriesAndItems(Guid listId, ShoppingList list)
    {
        _repoMock.Setup(r => r.GetByIdAsync(listId, default)).ReturnsAsync(list);
    }

    protected void VerifyBroadcast<T>(string expectedGroup, Func<T, bool> eventAssert) where T : ShoppingListEvent
    {
        _hubContextMock.Verify(
            h => h.Clients.Group(expectedGroup).SendAsync(
                "ShoppingListEvent",
                It.Is<T>(e => eventAssert(e)),
                default),
            Times.Once);
    }
}