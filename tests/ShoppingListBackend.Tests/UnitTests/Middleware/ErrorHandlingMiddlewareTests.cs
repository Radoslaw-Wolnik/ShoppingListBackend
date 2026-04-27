using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingListBackend.Api.Exceptions;
using ShoppingListBackend.Api.Middleware;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Middleware;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _loggerMock;
    private readonly ErrorHandlingMiddleware _middleware;

    public ErrorHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _middleware = new ErrorHandlingMiddleware(next => Task.CompletedTask, _loggerMock.Object);
    }

    private async Task<(int StatusCode, string Body)> InvokeMiddlewareWithException(Exception ex)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();
        var middleware = new ErrorHandlingMiddleware(_ => throw ex, _loggerMock.Object);
        await middleware.InvokeAsync(context);
        context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        var body = await new System.IO.StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task Handles_NotFoundException_Returns404()
    {
        var ex = new NotFoundException("Not found");
        var (status, body) = await InvokeMiddlewareWithException(ex);
        status.Should().Be(404);
        body.Should().Contain("Not Found");
    }

    [Fact]
    public async Task Handles_ForbiddenException_Returns403()
    {
        var ex = new ForbiddenException("Forbidden");
        var (status, body) = await InvokeMiddlewareWithException(ex);
        status.Should().Be(403);
        body.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task Handles_ValidationException_Returns400_WithErrors()
    {
        var failures = new[] { new FluentValidation.Results.ValidationFailure("Prop", "Error") };
        var ex = new ValidationException(failures);
        var (status, body) = await InvokeMiddlewareWithException(ex);
        status.Should().Be(400);
        body.Should().Contain("Validation Failed");
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task Handles_KeyNotFoundException_Returns404()
    {
        var ex = new KeyNotFoundException("Key missing");
        var (status, body) = await InvokeMiddlewareWithException(ex);
        status.Should().Be(404);
        body.Should().Contain("Not Found");
    }

    [Fact]
    public async Task Handles_UnauthorizedAccessException_Returns403()
    {
        var ex = new UnauthorizedAccessException("No permission");
        var (status, body) = await InvokeMiddlewareWithException(ex);
        status.Should().Be(403);
        body.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task Handles_GenericException_Returns500()
    {
        var ex = new Exception("Unexpected");
        var (status, body) = await InvokeMiddlewareWithException(ex);
        status.Should().Be(500);
        body.Should().Contain("Internal Server Error");
    }
}