using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NetMX.AspNetCore.Core.Exceptions;
using System.Text;
using System.Text.Json;

namespace NetMX.AspNetCore.Core.Tests.Exceptions;

public class NetMXExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoException_CallsNextMiddleware()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NetMXExceptionMiddleware>>();
        var context = new DefaultHttpContext();
        var nextCalled = false;
        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new NetMXExceptionMiddleware(Next, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NetMXExceptionMiddleware>>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        Task Next(HttpContext ctx) => throw new ArgumentException("Invalid argument");

        var middleware = new NetMXExceptionMiddleware(Next, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal("Invalid argument", response.GetProperty("error").GetProperty("message").GetString());
        Assert.Equal("ArgumentException", response.GetProperty("error").GetProperty("type").GetString());
        Assert.Equal(400, response.GetProperty("error").GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NetMXExceptionMiddleware>>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        Task Next(HttpContext ctx) => throw new UnauthorizedAccessException("Access denied");

        var middleware = new NetMXExceptionMiddleware(Next, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal("Access denied", response.GetProperty("error").GetProperty("message").GetString());
        Assert.Equal(401, response.GetProperty("error").GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NetMXExceptionMiddleware>>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        Task Next(HttpContext ctx) => throw new KeyNotFoundException("Resource not found");

        var middleware = new NetMXExceptionMiddleware(Next, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal("Resource not found", response.GetProperty("error").GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NetMXExceptionMiddleware>>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        Task Next(HttpContext ctx) => throw new InvalidOperationException("Something went wrong");

        var middleware = new NetMXExceptionMiddleware(Next, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal("Something went wrong", response.GetProperty("error").GetProperty("message").GetString());
        Assert.Equal("InvalidOperationException", response.GetProperty("error").GetProperty("type").GetString());
    }

    [Fact]
    public async Task InvokeAsync_Exception_LogsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<NetMXExceptionMiddleware>>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("Test error");
        Task Next(HttpContext ctx) => throw exception;

        var middleware = new NetMXExceptionMiddleware(Next, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
