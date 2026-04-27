using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingListBackend.Api.Data;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly Mock<AppDbContext> _contextMock;
    private readonly DeviceService _deviceService;

    public DeviceServiceTests()
    {
        _deviceRepoMock = new Mock<IDeviceRepository>();
        _contextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _deviceService = new DeviceService(_deviceRepoMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task GetDeviceAsync_ShouldReturnDevice_WhenExists()
    {
        var deviceId = Guid.NewGuid();
        var device = TestData.CreateDevice(deviceId);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync(device);

        var result = await _deviceService.GetDeviceAsync(deviceId);
        result.Should().Be(device);
    }

    [Fact]
    public async Task GetDeviceAsync_ShouldThrow_WhenNotFound()
    {
        var deviceId = Guid.NewGuid();
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync((Device?)null);

        Func<Task> act = () => _deviceService.GetDeviceAsync(deviceId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateUsernameAsync_ShouldUpdateDevice()
    {
        var deviceId = Guid.NewGuid();
        var device = TestData.CreateDevice(deviceId);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync(device);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        await _deviceService.UpdateUsernameAsync(deviceId, "NewName");

        device.UserName.Should().Be("NewName");
        _deviceRepoMock.Verify(x => x.Update(device), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateColourAsync_ShouldUpdateDevice()
    {
        var deviceId = Guid.NewGuid();
        var device = TestData.CreateDevice(deviceId);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync(device);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        await _deviceService.UpdateColourAsync(deviceId, "#00FF00");

        device.Colour.Should().Be("#00FF00");
        _deviceRepoMock.Verify(x => x.Update(device), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AddFriendAsync_ShouldAdd_WhenNotAlreadyFriends()
    {
        var deviceId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var device = TestData.CreateDevice(deviceId);
        var friend = TestData.CreateDevice(friendId);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync(device);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(friendId, default)).ReturnsAsync(friend);
        _deviceRepoMock.Setup(x => x.GetFriends(deviceId)).Returns(Enumerable.Empty<Device>().AsQueryable());
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        await _deviceService.AddFriendAsync(deviceId, friendId);

        _deviceRepoMock.Verify(x => x.AddFriend(deviceId, friendId), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AddFriendAsync_ShouldNotAdd_WhenAlreadyFriends()
    {
        var deviceId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var device = TestData.CreateDevice(deviceId);
        var friend = TestData.CreateDevice(friendId);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync(device);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(friendId, default)).ReturnsAsync(friend);
        _deviceRepoMock.Setup(x => x.GetFriends(deviceId)).Returns(new[] { friend }.AsQueryable());

        await _deviceService.AddFriendAsync(deviceId, friendId);

        _deviceRepoMock.Verify(x => x.AddFriend(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task AddFriendAsync_ShouldThrow_WhenFriendNotFound()
    {
        var deviceId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var device = TestData.CreateDevice(deviceId);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync(device);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(friendId, default)).ReturnsAsync((Device?)null);

        Func<Task> act = () => _deviceService.AddFriendAsync(deviceId, friendId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RemoveFriendAsync_ShouldRemove()
    {
        var deviceId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        // The repository has void RemoveFriend, not async
        _deviceRepoMock.Setup(x => x.RemoveFriend(deviceId, friendId));
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        await _deviceService.RemoveFriendAsync(deviceId, friendId);

        _deviceRepoMock.Verify(x => x.RemoveFriend(deviceId, friendId), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteDeviceAsync_ShouldDelete()
    {
        var deviceId = Guid.NewGuid();
        var device = TestData.CreateDevice(deviceId);
        _deviceRepoMock.Setup(x => x.GetByIdAsync(deviceId, default)).ReturnsAsync(device);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        await _deviceService.DeleteDeviceAsync(deviceId);

        _deviceRepoMock.Verify(x => x.Delete(device), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetFriendsAsync_ShouldReturnFriends()
    {
        var deviceId = Guid.NewGuid();
        var friends = new List<Device> { TestData.CreateDevice(), TestData.CreateDevice() };
        _deviceRepoMock.Setup(x => x.GetFriends(deviceId)).Returns(friends.AsQueryable());

        var result = await _deviceService.GetFriendsAsync(deviceId);
        result.Should().BeEquivalentTo(friends);
    }
}