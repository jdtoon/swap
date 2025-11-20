using Microsoft.AspNetCore.Http;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

/// <summary>
/// Tests SSE integration with HTMX SSE extension v2.2.4
/// https://htmx.org/extensions/sse/
/// </summary>
public class HtmxSseIntegrationTests
{
    [Fact]
    public async Task HtmxSseSwap_ReceivesCorrectEventName()
    {
        // This tests that event names used in HTMX sse-swap work correctly
        // HTML: <div sse-swap="todo-added">...</div>
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - send event that HTMX would listen for
        await stream.SendEventAsync("todo-added", "<li>New Todo Item</li>");
        
        // Assert
        var result = GetOutput(output);
        
        // HTMX sse-swap="todo-added" expects this exact event name
        Assert.Contains("event: todo-added\n", result);
        Assert.Contains("data: <li>New Todo Item</li>\n", result);
    }
    
    [Fact]
    public async Task HtmxMultipleSwapTargets_ReceiveSameEvent()
    {
        // Test multiple elements listening to same event
        // HTML: 
        // <div sse-swap="update">Target 1</div>
        // <div sse-swap="update">Target 2</div>
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("update", "<div>Updated Content</div>");
        
        // Assert
        var result = GetOutput(output);
        Assert.Contains("event: update\n", result);
        
        // Both targets would receive this event on the client
    }
    
    [Fact]
    public async Task HtmxDifferentEvents_SendSeparately()
    {
        // Test multiple event types for different targets
        // HTML:
        // <div sse-swap="task-created">...</div>
        // <div sse-swap="task-updated">...</div>
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("task-created", "<li class=\"new\">New Task</li>");
        await stream.SendEventAsync("task-updated", "<li class=\"updated\">Updated Task</li>");
        
        // Assert
        var result = GetOutput(output);
        
        // Should contain both events as separate messages
        Assert.Contains("event: task-created\n", result);
        Assert.Contains("data: <li class=\"new\">New Task</li>\n", result);
        Assert.Contains("event: task-updated\n", result);
        Assert.Contains("data: <li class=\"updated\">Updated Task</li>\n", result);
    }
    
    [Fact]
    public async Task HtmxPartialHtml_WithNewlines_WorksCorrectly()
    {
        // Real-world test: Razor partial with formatting
        // This is what you'd typically render from a controller
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - typical Razor partial output
        var razorHtml = @"<div class=""notification alert-success"">
    <h3>Task Completed!</h3>
    <p>The task has been marked as done.</p>
</div>";
        
        await stream.SendEventAsync("notification", razorHtml);
        
        // Assert
        var result = GetOutput(output);
        
        // Should have proper multi-line data format
        var lines = result.Split('\n', StringSplitOptions.None);
        Assert.Contains(lines, l => l == "event: notification");
        Assert.Contains(lines, l => l.StartsWith("data: <div"));
        Assert.Contains(lines, l => l == "data:     <h3>Task Completed!</h3>");
        
        // Critical: NO \r characters (would break EventSource parsing)
        Assert.DoesNotContain("\r", result);
    }
    
    [Fact]
    public async Task HtmxOobSwap_InSseData_WorksCorrectly()
    {
        // Test HTMX OOB swap within SSE data
        // The SSE event contains HTML with hx-swap-oob
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - HTML with OOB swap
        var html = @"<div id=""main-content"">
    <p>Main update</p>
</div>
<div id=""sidebar"" hx-swap-oob=""true"">
    <p>Sidebar update</p>
</div>";
        
        await stream.SendEventAsync("multi-update", html);
        
        // Assert
        var result = GetOutput(output);
        
        // HTMX will process hx-swap-oob attributes on the client
        Assert.Contains("hx-swap-oob", result);
        Assert.Contains("event: multi-update\n", result);
    }
    
    [Fact]
    public async Task HtmxConnectionEstablishment_SendsInitialEvent()
    {
        // Common pattern: send initial state when connection established
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - what you'd typically do in a controller
        await stream.SendEventAsync("connected", "<div>Connection established</div>");
        await stream.SendEventAsync("initial-data", "<div>Initial dashboard data</div>");
        
        // Assert
        var result = GetOutput(output);
        Assert.Contains("event: connected\n", result);
        Assert.Contains("event: initial-data\n", result);
    }
    
    [Fact]
    public async Task HtmxKeepAlive_MaintainsConnection()
    {
        // Test keep-alive pattern for long-lived connections
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("heartbeat", "");
        await stream.SendKeepAliveAsync();
        
        // Assert
        var result = GetOutput(output);
        
        // Heartbeat event (HTMX would receive but typically ignore)
        Assert.Contains("event: heartbeat\n", result);
        
        // Keep-alive comment (browser ignores, keeps connection alive)
        Assert.Contains(": keepalive\n", result);
    }
    
    [Fact]
    public async Task HtmxEventWithId_SupportsReconnection()
    {
        // Test Last-Event-ID for reconnection after disconnect
        // HTMX/browser will send Last-Event-ID header on reconnect
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("update", "<div>Update 1</div>", id: "1");
        await stream.SendEventAsync("update", "<div>Update 2</div>", id: "2");
        await stream.SendEventAsync("update", "<div>Update 3</div>", id: "3");
        
        // Assert
        var result = GetOutput(output);
        
        // Each event should have an ID
        Assert.Contains("id: 1\n", result);
        Assert.Contains("id: 2\n", result);
        Assert.Contains("id: 3\n", result);
        
        // Browser would send "Last-Event-ID: 3" header on reconnect
    }
    
    [Fact]
    public async Task HtmxComplexHtml_WithAttributes_EncodedCorrectly()
    {
        // Test complex HTML that HTMX would process
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - HTML with HTMX attributes
        var html = @"<div hx-get=""/tasks/1"" hx-trigger=""click"">
    <h3>Task Title</h3>
    <p>Description</p>
</div>";
        
        await stream.SendEventAsync("task-detail", html);
        
        // Assert
        var result = GetOutput(output);
        
        // HTMX attributes should be preserved
        Assert.Contains("hx-get", result);
        Assert.Contains("hx-trigger", result);
    }
    
    [Fact]
    public async Task HtmxRetryDirective_ConfiguresReconnection()
    {
        // Test retry directive for automatic reconnection
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - set retry to 5 seconds (5000ms)
        await stream.SendRetryDirectiveAsync(5000);
        
        // Assert
        var result = GetOutput(output);
        
        // Browser will wait 5 seconds before reconnecting
        Assert.Equal("retry: 5000\n\n", result);
    }
    
    [Theory]
    [InlineData("task-created")]
    [InlineData("task-updated")]
    [InlineData("task-deleted")]
    [InlineData("notification")]
    [InlineData("toast")]
    [InlineData("refresh")]
    public async Task HtmxCommonEventNames_WorkCorrectly(string eventName)
    {
        // Test common event naming patterns used with HTMX
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync(eventName, $"<div>{eventName}</div>");
        
        // Assert
        var result = GetOutput(output);
        Assert.Contains($"event: {eventName}\n", result);
    }
    
    // Helper methods
    
    private static (ServerSentEventStream stream, MemoryStream output) CreateTestStream()
    {
        var context = new DefaultHttpContext();
        var output = new MemoryStream();
        context.Response.Body = output;
        
        var stream = new ServerSentEventStream(context.Response, CancellationToken.None);
        return (stream, output);
    }
    
    private static string GetOutput(MemoryStream stream)
    {
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
