using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Swap.Htmx.Results;
using Swap.Htmx.Realtime;
using Xunit;

namespace Swap.Htmx.Tests.ServerSentEvents;

public class SwapSseResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_RespectsCanJoinRoom_WhenAllowed()
    {
        // Arrange
        var mockRegistry = new Mock<ISseConnectionRegistry>();
        var context = new DefaultHttpContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user1") }, "TestAuth"));
        context.User = user;
        
        var actionContext = new ActionContext { HttpContext = context };
        
        var result = new SwapSseResult(mockRegistry.Object, options =>
        {
            options.AutoSubscribeRooms = new[] { "admin-room" };
            options.CanJoinRoom = (conn, room) => Task.FromResult(true);
        });

        // Act
        var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;
        cts.CancelAfter(100); // Cancel quickly to break the keep-alive loop

        try
        {
            await result.ExecuteAsync(context);
        }
        catch (OperationCanceledException)
        {
            // Expected behavior when connection is aborted
        }

        // Assert
        mockRegistry.Verify(r => r.RegisterConnection(It.Is<SseConnection>(c => 
            c.Rooms.Contains("admin-room")
        )), Times.Once);
    }

    [Fact]
    public async Task ExecuteResultAsync_RespectsCanJoinRoom_WhenDenied()
    {
        // Arrange
        var mockRegistry = new Mock<ISseConnectionRegistry>();
        var context = new DefaultHttpContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user1") }, "TestAuth"));
        context.User = user;
        
        var actionContext = new ActionContext { HttpContext = context };
        
        var result = new SwapSseResult(mockRegistry.Object, options =>
        {
            options.AutoSubscribeRooms = new[] { "admin-room" };
            options.CanJoinRoom = (conn, room) => Task.FromResult(false);
        });

        // Act
        var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;
        cts.CancelAfter(100); // Cancel quickly to break the keep-alive loop

        try
        {
            await result.ExecuteAsync(context);
        }
        catch (OperationCanceledException)
        {
            // Expected behavior when connection is aborted
        }

        // Assert
        mockRegistry.Verify(r => r.RegisterConnection(It.Is<SseConnection>(c => 
            !c.Rooms.Contains("admin-room")
        )), Times.Once);
    }
}
