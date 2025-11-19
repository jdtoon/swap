using System.Text;
using Microsoft.AspNetCore.Http;
using Swap.Htmx.ServerSentEvents;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

/// <summary>
/// Tests SSE message formatting per W3C EventSource specification
/// and HTMX SSE extension compatibility
/// </summary>
public class ServerSentEventStreamTests
{
    [Fact]
    public async Task SendEventAsync_SingleLine_MatchesW3CSpec()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("myevent", "<div>Hello</div>");
        
        // Assert - exact W3C format
        var result = GetOutput(output);
        Assert.Equal(
            "event: myevent\n" +
            "data: <div>Hello</div>\n" +
            "\n",
            result
        );
    }
    
    [Fact]
    public async Task SendEventAsync_WithEventId_IncludesIdField()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("test", "<div>Content</div>", id: "msg-123");
        
        // Assert
        var result = GetOutput(output);
        Assert.Equal(
            "id: msg-123\n" +
            "event: test\n" +
            "data: <div>Content</div>\n" +
            "\n",
            result
        );
    }
    
    [Fact]
    public async Task SendEventAsync_MultiLineWithCRLF_NormalizesToLF()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - Windows line endings (common in Razor views)
        await stream.SendEventAsync("test", "<div>\r\n  <p>Line 1</p>\r\n  <p>Line 2</p>\r\n</div>");
        
        // Assert - normalized to LF only
        var result = GetOutput(output);
        Assert.Equal(
            "event: test\n" +
            "data: <div>\n" +
            "data:   <p>Line 1</p>\n" +
            "data:   <p>Line 2</p>\n" +
            "data: </div>\n" +
            "\n",
            result
        );
        
        // Verify NO \r characters in output (critical for SSE spec compliance)
        Assert.DoesNotContain("\r", result);
    }
    
    [Fact]
    public async Task SendEventAsync_MultiLineWithLF_FormatsCorrectly()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - Unix line endings
        await stream.SendEventAsync("test", "<div>\n  <p>Line 1</p>\n  <p>Line 2</p>\n</div>");
        
        // Assert
        var result = GetOutput(output);
        Assert.Equal(
            "event: test\n" +
            "data: <div>\n" +
            "data:   <p>Line 1</p>\n" +
            "data:   <p>Line 2</p>\n" +
            "data: </div>\n" +
            "\n",
            result
        );
        Assert.DoesNotContain("\r", result);
    }
    
    [Fact]
    public async Task SendEventAsync_MixedLineEndings_NormalizesAll()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - mixed \r\n, \n, and \r
        await stream.SendEventAsync("test", "Line1\r\nLine2\nLine3\rLine4");
        
        // Assert - all normalized to \n
        var result = GetOutput(output);
        Assert.Equal(
            "event: test\n" +
            "data: Line1\n" +
            "data: Line2\n" +
            "data: Line3\n" +
            "data: Line4\n" +
            "\n",
            result
        );
        Assert.DoesNotContain("\r", result);
    }
    
    [Fact]
    public async Task SendEventAsync_RazorPartialHtml_WorksCorrectly()
    {
        // Real-world test: typical Razor partial output with formatting
        
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - typical Razor partial output (Windows line endings)
        var razorHtml = @"<div class=""alert alert-success"">
    <h3>Success!</h3>
    <p>Operation completed</p>
</div>";
        
        await stream.SendEventAsync("notification", razorHtml);
        
        // Assert
        var result = GetOutput(output);
        
        // Should have proper multi-line data format
        var lines = result.Split('\n', StringSplitOptions.None);
        Assert.Contains(lines, l => l == "event: notification");
        Assert.Contains(lines, l => l.StartsWith("data: <div"));
        Assert.Contains(lines, l => l == "data:     <h3>Success!</h3>");
        
        // NO \r characters should exist
        Assert.DoesNotContain("\r", result);
    }
    
    [Fact]
    public async Task SendKeepAliveAsync_MatchesW3CCommentSpec()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendKeepAliveAsync();
        
        // Assert - comments start with ':'
        var result = GetOutput(output);
        Assert.Equal(": keepalive\n\n", result);
    }
    
    [Fact]
    public async Task SendRetryDirectiveAsync_FormatsCorrectly()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendRetryDirectiveAsync(5000);
        
        // Assert
        var result = GetOutput(output);
        Assert.Equal("retry: 5000\n\n", result);
    }
    
    [Fact]
    public async Task SendEventAsync_MultipleEvents_SeparatedCorrectly()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("event1", "data1");
        await stream.SendEventAsync("event2", "data2");
        
        // Assert - blank line between messages
        var result = GetOutput(output);
        Assert.Equal(
            "event: event1\n" +
            "data: data1\n" +
            "\n" +
            "event: event2\n" +
            "data: data2\n" +
            "\n",
            result
        );
    }
    
    [Fact]
    public async Task SendEventAsync_EmptyData_ValidSseMessage()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync("empty", "");
        
        // Assert
        var result = GetOutput(output);
        Assert.Equal(
            "event: empty\n" +
            "data: \n" +
            "\n",
            result
        );
    }
    
    [Fact]
    public async Task SendEventAsync_SpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act - Unicode and emoji
        await stream.SendEventAsync("test", "<div>Emoji: 🚀 Unicode: café</div>");
        
        // Assert
        var result = GetOutput(output);
        Assert.Contains("🚀", result);
        Assert.Contains("café", result);
    }
    
    [Theory]
    [InlineData("simple", "no-newlines")]
    [InlineData("test-123", "data-here")]
    [InlineData("my_event", "test_data")]
    [InlineData("task-updated", "<li>New Task</li>")]
    public async Task SendEventAsync_EventNames_ArePreserved(string eventName, string data)
    {
        // Arrange
        var (stream, output) = CreateTestStream();
        
        // Act
        await stream.SendEventAsync(eventName, data);
        
        // Assert
        var result = GetOutput(output);
        Assert.StartsWith($"event: {eventName}\n", result);
    }
    
    [Fact]
    public async Task SendEventAsync_NullEventName_ThrowsArgumentException()
    {
        // Arrange
        var (stream, _) = CreateTestStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await stream.SendEventAsync(null!, "<div>Test</div>")
        );
    }
    
    [Fact]
    public async Task SendEventAsync_EmptyEventName_ThrowsArgumentException()
    {
        // Arrange
        var (stream, _) = CreateTestStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await stream.SendEventAsync("", "<div>Test</div>")
        );
    }
    
    [Fact]
    public async Task SendEventAsync_NullHtml_ThrowsArgumentNullException()
    {
        // Arrange
        var (stream, _) = CreateTestStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await stream.SendEventAsync("test", null!)
        );
    }
    
    [Fact]
    public async Task SendEventAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (stream, _) = CreateTestStream();
        await stream.DisposeAsync();
        
        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await stream.SendEventAsync("test", "<div>Test</div>")
        );
    }
    
    [Fact]
    public async Task SendKeepAliveAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (stream, _) = CreateTestStream();
        await stream.DisposeAsync();
        
        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await stream.SendKeepAliveAsync()
        );
    }
    
    [Fact]
    public async Task SendEventAsync_FlushesImmediately()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new TestFlushableStream();
        context.Response.Body = responseStream;
        
        var stream = new ServerSentEventStream(context.Response, CancellationToken.None);
        
        // Act
        await stream.SendEventAsync("test", "<div>Test</div>");
        
        // Assert
        Assert.True(responseStream.WasFlushed);
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
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

/// <summary>
/// Helper class to test immediate flushing behavior
/// </summary>
internal class TestFlushableStream : MemoryStream
{
    public bool WasFlushed { get; private set; }
    
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        WasFlushed = true;
        await base.FlushAsync(cancellationToken);
    }
}
