using Microsoft.AspNetCore.SignalR;
using Moq;
using ShoppingListBackend.Api.Hubs;
using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Api.Services;

namespace ShoppingListBackend.Tests.Helpers;

public static class MockSetup
{
    public static Mock<IDeviceRepository> CreateDeviceRepositoryMock()
    {
        return new Mock<IDeviceRepository>();
    }

    public static Mock<IHashService> CreateHashServiceMock()
    {
        return new Mock<IHashService>();
    }

    public static Mock<IShoppingListRepository> CreateShoppingListRepositoryMock()
    {
        return new Mock<IShoppingListRepository>();
    }

    public static Mock<IHubContext<ShoppingListHub>> CreateHubContextMock()
    {
        return new Mock<IHubContext<ShoppingListHub>>();
    }
}