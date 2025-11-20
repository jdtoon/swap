using Microsoft.AspNetCore.Http;
using Swap.Htmx.Realtime;
using Xunit;
using System.Security.Claims;

namespace Swap.Htmx.Tests.ServerSentEvents;

public class SseConnectionTests
{
    [Fact]
    public void SseConnection_HasUniqueId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        
        // Act
        var connection1 = new SseConnection("id1", stream, httpContext);
        var connection2 = new SseConnection("id2", stream, httpContext);

        // Assert
        Assert.NotEqual(connection1.Id, connection2.Id);
    }

    [Fact]
    public void SseConnection_TracksUser()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user123") },
            "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        
        // Act
        var connection = new SseConnection("test-id", stream, httpContext);

        // Assert
        Assert.NotNull(connection.User);
        Assert.Equal("user123", connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void SseConnection_JoinRoom_AddsToRooms()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        var connection = new SseConnection("test-id", stream, httpContext);

        // Act
        connection.JoinRoom("team-alpha");
        connection.JoinRoom("dashboard");

        // Assert
        Assert.True(connection.IsInRoom("team-alpha"));
        Assert.True(connection.IsInRoom("dashboard"));
        Assert.False(connection.IsInRoom("other-room"));
    }

    [Fact]
    public void SseConnection_SubscribeToEvent_AddsToFilters()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        var connection = new SseConnection("test-id", stream, httpContext);

        // Act
        connection.SubscribeToEvent("task-update");
        connection.SubscribeToEvent("notification");

        // Assert
        Assert.True(connection.IsSubscribedToEvent("task-update"));
        Assert.True(connection.IsSubscribedToEvent("notification"));
        Assert.False(connection.IsSubscribedToEvent("other-event"));
    }

    [Fact]
    public async Task SseConnection_SendEventAsync_WritesToStream()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        var connection = new SseConnection("test-id", stream, httpContext);

        // Act
        await connection.SendEventAsync("test-event", "<div>Test Content</div>");

        // Assert
        memoryStream.Position = 0;
        var reader = new StreamReader(memoryStream);
        var output = await reader.ReadToEndAsync();
        
        Assert.Contains("event: test-event", output);
        Assert.Contains("data: <div>Test Content</div>", output);
    }

    [Fact]
    public void SseConnection_RecordsConnectedAt()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        
        var before = DateTime.UtcNow;

        // Act
        var connection = new SseConnection("test-id", stream, httpContext);

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(connection.ConnectedAt, before, after);
    }
}

