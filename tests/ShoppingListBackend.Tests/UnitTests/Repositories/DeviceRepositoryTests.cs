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

public class DeviceRepositoryTests : TestBase
{
    private readonly DeviceRepository _repository;

    public DeviceRepositoryTests()
    {
        _repository = new DeviceRepository(_context);
    }

    [Fact]
    public async Task Add_And_GetByIdAsync_Works()
    {
        var device = TestData.CreateDevice();
        _repository.Add(device);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(device.Id);
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(device.Id);
    }

    [Fact]
    public async Task GetByApiKeySha256Async_ReturnsDevice_WhenExists()
    {
        var device = TestData.CreateDevice();
        device.ApiKeySha256 = "testsha256";
        _repository.Add(device);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetByApiKeySha256Async("testsha256");
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(device.Id);
    }

    [Fact]
    public async Task GetByApiKeySha256Async_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetByApiKeySha256Async("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_Device_Works()
    {
        var device = TestData.CreateDevice();
        _repository.Add(device);
        await _context.SaveChangesAsync();

        device.UserName = "UpdatedName";
        _repository.Update(device);
        await _context.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(device.Id);
        updated!.UserName.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task Delete_Device_Works()
    {
        var device = TestData.CreateDevice();
        _repository.Add(device);
        await _context.SaveChangesAsync();

        _repository.Delete(device);
        await _context.SaveChangesAsync();

        var deleted = await _repository.GetByIdAsync(device.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task AddFriend_And_GetFriends_Works()
    {
        var device = TestData.CreateDevice();
        var friend = TestData.CreateDevice();
        _repository.Add(device);
        _repository.Add(friend);
        await _context.SaveChangesAsync();

        _repository.AddFriend(device.Id, friend.Id);
        await _context.SaveChangesAsync();

        var friendsQuery = _repository.GetFriends(device.Id);
        var friends = await friendsQuery.ToListAsync();
        friends.Should().ContainSingle(f => f.Id == friend.Id);
    }

    [Fact]
    public async Task RemoveFriend_Works()
    {
        var device = TestData.CreateDevice();
        var friend = TestData.CreateDevice();
        _repository.Add(device);
        _repository.Add(friend);
        _repository.AddFriend(device.Id, friend.Id);
        await _context.SaveChangesAsync();

        _repository.RemoveFriend(device.Id, friend.Id);
        await _context.SaveChangesAsync();

        var friends = await _repository.GetFriends(device.Id).ToListAsync();
        friends.Should().BeEmpty();
    }
}