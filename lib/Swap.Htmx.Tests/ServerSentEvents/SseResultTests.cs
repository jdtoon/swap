using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

/// <summary>
/// Tests for SSE Result classes that establish SSE connections
/// </summary>
public class SseResultTests
{
    [Fact]
    public async Task ServerSentEventsResult_SetsCorrectHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var actionContext = new ActionContext { HttpContext = context };
        
        bool handlerCalled = false;
        var result = new ServerSentEventsResult((stream, ct) =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });
        
        // Act
        await result.ExecuteResultAsync(actionContext);
        
        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("text/event-stream; charset=utf-8", context.Response.ContentType);
        Assert.Equal("no-cache", context.Response.Headers["Cache-Control"].ToString());
        Assert.Equal("keep-alive", context.Response.Headers["Connection"].ToString());
        Assert.Equal("no", context.Response.Headers["X-Accel-Buffering"].ToString());
        Assert.True(handlerCalled);
    }
    
    [Fact]
    public async Task EnhancedServerSentEventsResult_SetsCorrectHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton<ISseConnectionRegistry>(new SseConnectionRegistry(
            new InMemorySseBackplane(NullLogger<InMemorySseBackplane>.Instance),
            NullLogger<SseConnectionRegistry>.Instance));
        context.RequestServices = services.BuildServiceProvider();
        
        var actionContext = new ActionContext { HttpContext = context };
        
        bool handlerCalled = false;
        var result = new EnhancedServerSentEventsResult((builder, ct) =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });
        
        // Act
        await result.ExecuteResultAsync(actionContext);
        
        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("text/event-stream; charset=utf-8", context.Response.ContentType);
        Assert.Equal("no-cache", context.Response.Headers["Cache-Control"].ToString());
        Assert.Equal("keep-alive", context.Response.Headers["Connection"].ToString());
        Assert.Equal("no", context.Response.Headers["X-Accel-Buffering"].ToString());
        Assert.True(handlerCalled);
    }
    
    [Fact]
    public async Task ServerSentEventsResult_DisposesStreamAfterCompletion()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var actionContext = new ActionContext { HttpContext = context };
        
        ServerSentEventStream? capturedStream = null;
        var result = new ServerSentEventsResult((stream, ct) =>
        {
            capturedStream = stream;
            return Task.CompletedTask;
        });
        
        // Act
        await result.ExecuteResultAsync(actionContext);
        
        // Assert - stream should be disposed
        Assert.NotNull(capturedStream);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await capturedStream.SendEventAsync("test", "data")
        );
    }
    
    [Fact]
    public async Task ServerSentEventsResult_PassesRequestAbortedToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;
        var actionContext = new ActionContext { HttpContext = context };
        
        CancellationToken capturedToken = default;
        var result = new ServerSentEventsResult((stream, ct) =>
        {
            capturedToken = ct;
            return Task.CompletedTask;
        });
        
        // Act
        await result.ExecuteResultAsync(actionContext);
        
        // Assert
        Assert.Equal(cts.Token, capturedToken);
    }
    
    [Fact]
    public async Task ServerSentEventsResult_HandlesHandlerException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var actionContext = new ActionContext { HttpContext = context };
        
        var result = new ServerSentEventsResult((stream, ct) =>
        {
            throw new InvalidOperationException("Test exception");
        });
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await result.ExecuteResultAsync(actionContext)
        );
    }
    
    [Fact]
    public async Task ServerSentEventsResult_HandlesCancellation()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel
        context.RequestAborted = cts.Token;
        var actionContext = new ActionContext { HttpContext = context };
        
        var result = new ServerSentEventsResult(async (stream, ct) =>
        {
            await Task.Delay(1000, ct); // Should throw OperationCanceledException
        });
        
        // Act - should NOT throw, just complete
        await result.ExecuteResultAsync(actionContext);
        
        // Assert - should complete successfully
        // (The implementation catches OperationCanceledException as normal disconnection)
    }
}
