using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Swap.Htmx.ServerSentEvents;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

public class SseConnectionRegistryTests
{
    [Fact]
    public void RegisterConnection_DoesNotThrow()
    {
        // Arrange
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var registry = new SseConnectionRegistry(logger);
        
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);
        var connection = new SseConnection("test-id", stream, httpContext);

        // Act & Assert - should not throw
        registry.RegisterConnection(connection);
    }

    [Fact]
    public void UnregisterConnection_DoesNotThrowForNonexistentConnection()
    {
        // Arrange
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var registry = new SseConnectionRegistry(logger);

        // Act & Assert - should not throw
        registry.UnregisterConnection("nonexistent-id");
    }

    [Fact]
    public async Task BroadcastAsync_SendsToAllConnections()
    {
        // Arrange
        var logger = new Mock<ILogger<SseConnectionRegistry>>().Object;
        var registry = new SseConnectionRegistry(logger);
        
        var stream1 = new MemoryStream();
        var stream2 = new MemoryStream();
        
        var httpContext1 = new DefaultHttpContext();
        httpContext1.Response.Body = stream1;
        var sseStream1 = new ServerSentEventStream(httpContext1.Response, CancellationToken.None);
        var connection1 = new SseConnection("conn1", sseStream1, httpContext1);
        
        var httpContext2 = new DefaultHttpContext();
        httpContext2.Response.Body = stream2;
        var sseStream2 = new ServerSentEventStream(httpContext2.Response, CancellationToken.None);
        var connection2 = new SseConnection("conn2", sseStream2, httpContext2);
        
        registry.RegisterConnection(connection1);
        registry.RegisterConnection(connection2);

        // Act
        await registry.BroadcastAsync("test-event", "<div>Content</div>");

        // Assert
        stream1.Position = 0;
        var reader1 = new StreamReader(stream1);
        var output1 = await reader1.ReadToEndAsync();
        Assert.Contains("data: <div>Content</div>", output1);

        stream2.Position = 0;
        var reader2 = new StreamReader(stream2);
        var output2 = await reader2.ReadToEndAsync();
        Assert.Contains("data: <div>Content</div>", output2);
    }
}
