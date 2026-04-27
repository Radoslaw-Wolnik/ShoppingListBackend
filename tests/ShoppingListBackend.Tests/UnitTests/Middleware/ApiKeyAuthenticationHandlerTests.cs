using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ShoppingListBackend.Api.Middleware;
using ShoppingListBackend.Api.Models;
using ShoppingListBackend.Api.Services;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Middleware;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly ApiKeyAuthenticationHandler _handler;
    private readonly DefaultHttpContext _context;

    public ApiKeyAuthenticationHandlerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());
        var loggerFactory = new Mock<ILoggerFactory>();
        var logger = new Mock<ILogger<ApiKeyAuthenticationHandler>>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        _handler = new ApiKeyAuthenticationHandler(
            optionsMonitor.Object,
            loggerFactory.Object,
            System.Text.Encodings.Web.UrlEncoder.Default,
            _authServiceMock.Object);

        _context = new DefaultHttpContext();
        _handler.InitializeAsync(
            new AuthenticationScheme("ApiKey", null, typeof(ApiKeyAuthenticationHandler)),
            _context).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ReturnsNoResult_WhenNoApiKeyProvided()
    {
        var result = await _handler.AuthenticateAsync();
        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ReturnsSuccess_WhenHeaderApiKeyValid()
    {
        var deviceId = Guid.NewGuid();
        var device = new Device { Id = deviceId, UserName = "TestUser" };
        _authServiceMock.Setup(x => x.ValidateApiKeyAsync("valid-key")).ReturnsAsync(device);
        _context.Request.Headers["X-API-Key"] = "valid-key";

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(deviceId.ToString());
        result.Principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be("TestUser");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ReturnsSuccess_WhenQueryApiKeyValid()
    {
        var deviceId = Guid.NewGuid();
        var device = new Device { Id = deviceId, UserName = "TestUser" };
        _authServiceMock.Setup(x => x.ValidateApiKeyAsync("valid-key")).ReturnsAsync(device);
        _context.Request.QueryString = new QueryString("?apiKey=valid-key");

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(deviceId.ToString());
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ReturnsFail_WhenInvalidApiKey()
    {
        _authServiceMock.Setup(x => x.ValidateApiKeyAsync("invalid")).ReturnsAsync((Device?)null);
        _context.Request.Headers["X-API-Key"] = "invalid";

        var result = await _handler.AuthenticateAsync();

        result.Failure.Should().NotBeNull();
        result.Failure!.Message.Should().Be("Invalid API key");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_UsesHeaderFirst_ThenQuery()
    {
        var deviceId = Guid.NewGuid();
        var device = new Device { Id = deviceId };
        _authServiceMock.Setup(x => x.ValidateApiKeyAsync("header-key")).ReturnsAsync(device);
        _context.Request.Headers["X-API-Key"] = "header-key";
        _context.Request.QueryString = new QueryString("?apiKey=query-key");

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        _authServiceMock.Verify(x => x.ValidateApiKeyAsync("header-key"), Times.Once);
        _authServiceMock.Verify(x => x.ValidateApiKeyAsync("query-key"), Times.Never);
    }
}