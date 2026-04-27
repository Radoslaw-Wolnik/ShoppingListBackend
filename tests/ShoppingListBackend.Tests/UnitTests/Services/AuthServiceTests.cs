using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingListBackend.Api.Repositories;
using ShoppingListBackend.Api.Services;
using ShoppingListBackend.Tests.Helpers;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public class AuthServiceTests : TestBase
{
    private readonly Mock<IHashService> _hashServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _hashServiceMock = new Mock<IHashService>();
        _authService = new AuthService(new DeviceRepository(_context), _hashServiceMock.Object, _context);
    }

    [Fact]
    public async Task RegisterDeviceAsync_ShouldCreateDeviceAndReturnApiKey()
    {
        // Arrange
        _hashServiceMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashedKey");

        // Act
        var result = await _authService.RegisterDeviceAsync();

        // Assert
        result.Should().NotBeNull();
        result.ApiKey.Should().NotBeNullOrEmpty();
        result.DeviceId.Should().NotBeEmpty();

        var savedDevice = await _context.Devices.FindAsync(result.DeviceId);
        savedDevice.Should().NotBeNull();
        savedDevice.ApiKeyHash.Should().Be("hashedKey");
        savedDevice.ApiKeySha256.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ShouldReturnDevice_WhenValid()
    {
        // Arrange
        var device = TestData.CreateDevice();
        var apiKey = "validKey";
        var sha256 = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiKey)));
        device.ApiKeySha256 = sha256;
        device.ApiKeyHash = "hashedKey";
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        _hashServiceMock.Setup(x => x.Verify(apiKey, device.ApiKeyHash)).Returns(true);

        // Act
        var result = await _authService.ValidateApiKeyAsync(apiKey);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(device.Id);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ShouldReturnNull_WhenApiKeyNotFound()
    {
        // Act
        var result = await _authService.ValidateApiKeyAsync("invalidKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ShouldReturnNull_WhenHashDoesNotMatch()
    {
        // Arrange
        var device = TestData.CreateDevice();
        var apiKey = "validKey";
        var sha256 = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiKey)));
        device.ApiKeySha256 = sha256;
        device.ApiKeyHash = "hashedKey";
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        _hashServiceMock.Setup(x => x.Verify(apiKey, device.ApiKeyHash)).Returns(false);

        // Act
        var result = await _authService.ValidateApiKeyAsync(apiKey);

        // Assert
        result.Should().BeNull();
    }
}