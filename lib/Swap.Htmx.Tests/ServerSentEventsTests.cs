using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.ServerSentEvents;
using Xunit;

namespace Swap.Htmx.Tests;

public class ServerSentEventsTests
{
    [Fact]
    public async Task ServerSentEventStream_SendsFormattedEvent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);

        // Act
        await stream.SendEventAsync("test-event", "<div>Hello</div>");

        // Assert
        memoryStream.Position = 0;
        var reader = new StreamReader(memoryStream);
        var output = await reader.ReadToEndAsync();

        Assert.Contains("event: test-event", output);
        Assert.Contains("data: <div>Hello</div>", output);
        Assert.EndsWith("\n\n", output); // SSE messages end with double newline
    }

    [Fact]
    public async Task ServerSentEventStream_HandlesMultilineHtml()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);

        // Act
        var html = "<div>\n  <p>Multi-line</p>\n</div>";
        await stream.SendEventAsync("multi", html);

        // Assert
        memoryStream.Position = 0;
        var reader = new StreamReader(memoryStream);
        var output = await reader.ReadToEndAsync();

        // Each line should have "data: " prefix
        var lines = output.Split('\n');
        Assert.Contains(lines, l => l == "data: <div>");
        Assert.Contains(lines, l => l == "data:   <p>Multi-line</p>");
        Assert.Contains(lines, l => l == "data: </div>");
    }

    [Fact]
    public async Task ServerSentEventsResult_SetsCorrectHeaders()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        var result = new ServerSentEventsResult(async (stream, ct) =>
        {
            await stream.SendEventAsync("test", "<div>Test</div>");
        });

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal("text/event-stream", httpContext.Response.ContentType);
        Assert.Equal("no-cache", httpContext.Response.Headers["Cache-Control"]);
        Assert.Equal("keep-alive", httpContext.Response.Headers["Connection"]);
    }

    [Fact]
    public async Task ServerSentEvents_ThrowsOnNullHandler()
    {
        Assert.Throws<ArgumentNullException>(() => new ServerSentEventsResult(null!));
    }

    [Fact]
    public async Task SendEventAsync_ThrowsOnEmptyEventName()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            stream.SendEventAsync("", "<div>Test</div>"));
    }

    [Fact]
    public async Task SendKeepAliveAsync_SendsComment()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        var stream = new ServerSentEventStream(httpContext.Response, CancellationToken.None);

        // Act
        await stream.SendKeepAliveAsync();

        // Assert
        memoryStream.Position = 0;
        var reader = new StreamReader(memoryStream);
        var output = await reader.ReadToEndAsync();

        Assert.Equal(": keepalive\n\n", output);
    }
}
