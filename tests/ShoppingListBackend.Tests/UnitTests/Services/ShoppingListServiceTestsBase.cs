using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.RealTime;
using ShoppingListBackend.Api.Hubs;
using ShoppingListBackend.Api.Mappers;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Tests.Helpers;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public abstract class ShoppingListServiceTestsBase : TestBase
{
    protected readonly Mock<IShoppingListRepository> _repoMock;
    protected readonly Mock<IHubContext<ShoppingListHub>> _hubContextMock;
    protected readonly Mock<IHubClients> _clientsMock;
    protected readonly Mock<IClientProxy> _clientProxyMock;
    protected readonly ShoppingListService _service;

    protected ShoppingListServiceTestsBase()
    {
        _repoMock = new Mock<IShoppingListRepository>();
        _hubContextMock = new Mock<IHubContext<ShoppingListHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

        _service = new ShoppingListService(
            _repoMock.Object,
            _context,
            _hubContextMock.Object,
            mapper
        );

        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
    }

    protected void SetupGetListWithCategoriesAndItems(Guid listId, ShoppingList list)
    {
        _repoMock.Setup(r => r.GetByIdAsync(listId, default)).ReturnsAsync(list);
    }

    protected void VerifyBroadcast<T>(string expectedGroup, Func<T, bool> eventAssert) where T : ShoppingListEvent
    {
        _clientsMock.Verify(c => c.Group(expectedGroup), Times.AtLeastOnce);
        _clientProxyMock.Verify(
            proxy => proxy.SendCoreAsync(
                "ShoppingListEvent",
                It.Is<object?[]>(args => MatchesEvent(args, eventAssert)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static bool MatchesEvent<T>(object?[] args, Func<T, bool> eventAssert) where T : ShoppingListEvent
        => args.Length == 1 && args[0] is T typedEvent && eventAssert(typedEvent);
}
