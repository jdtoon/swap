using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace Swap.Htmx.WebSockets;

/// <summary>
/// Base class for handling WebSocket connections with lifecycle hooks.
/// Inherit from this class to implement custom WebSocket behavior.
/// </summary>
public abstract class SwapWebSocketHandler
{
    /// <summary>
    /// Gets the HTTP context associated with the WebSocket connection.
    /// </summary>
    internal HttpContext? HttpContext { get; private set; }

    /// <summary>
    /// Called when a new WebSocket connection is established.
    /// Override this to perform initialization logic.
    /// </summary>
    /// <param name="connection">The WebSocket connection.</param>
    protected virtual Task OnConnectedAsync(WebSocketConnection connection)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a message is received from the client.
    /// Override this to handle incoming messages.
    /// </summary>
    /// <param name="connection">The WebSocket connection.</param>
    /// <param name="message">The received message.</param>
    protected virtual Task OnMessageAsync(WebSocketConnection connection, string message)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the WebSocket connection is closed or lost.
    /// Override this to perform cleanup logic.
    /// </summary>
    /// <param name="connection">The WebSocket connection.</param>
    protected virtual Task OnDisconnectedAsync(WebSocketConnection connection)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the WebSocket connection lifecycle.
    /// This is called by the middleware to process the WebSocket.
    /// </summary>
    /// <param name="context">The HTTP context containing the WebSocket.</param>
    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        HttpContext = context; // Store context for view rendering
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connection = new WebSocketConnection(webSocket);

        try
        {
            await OnConnectedAsync(connection);

            while (connection.IsOpen)
            {
                var message = await connection.ReceiveAsync(context.RequestAborted);
                
                if (message == null)
                    break; // Connection closing

                await OnMessageAsync(connection, message);
            }
        }
        catch (WebSocketException)
        {
            // Connection lost - this is normal
        }
        catch (OperationCanceledException)
        {
            // Request cancelled - this is normal during shutdown
        }
        finally
        {
            await OnDisconnectedAsync(connection);
            
            if (connection.IsOpen)
            {
                await connection.CloseAsync();
            }
        }
    }
}
