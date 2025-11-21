using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Realtime;

namespace Swap.Htmx.Results;

/// <summary>
/// Options for configuring a WebSocket connection result.
/// </summary>
public class SwapWebSocketOptions
{
    /// <summary>
    /// A list of rooms to automatically join upon connection.
    /// </summary>
    public IEnumerable<string> AutoSubscribeRooms { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// A list of events to automatically subscribe to upon connection.
    /// </summary>
    public IEnumerable<string> AutoSubscribeEvents { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Optional callback to execute when the connection is established.
    /// </summary>
    public Func<WebSocketConnection, Task>? OnConnected { get; set; }

    /// <summary>
    /// Optional callback to execute when the connection is disconnected.
    /// </summary>
    public Func<string, Task>? OnDisconnected { get; set; }

    /// <summary>
    /// Optional validator to check if a user is allowed to join a specific room.
    /// If provided, this will be called for each room in AutoSubscribeRooms.
    /// Return true to allow, false to deny.
    /// </summary>
    public Func<WebSocketConnection, string, Task<bool>>? CanJoinRoom { get; set; }
}

/// <summary>
/// An IResult implementation that establishes and maintains a WebSocket connection.
/// </summary>
public class SwapWebSocketResult : IResult
{
    private readonly IRealtimeConnectionRegistry _registry;
    private readonly SwapWebSocketOptions _options;

    public SwapWebSocketResult(IRealtimeConnectionRegistry registry, Action<SwapWebSocketOptions>? configure = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = new SwapWebSocketOptions();
        configure?.Invoke(_options);
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket request expected.");
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        var connectionId = Guid.NewGuid().ToString("N");
        var connection = new WebSocketConnection(connectionId, webSocket, context);

        _registry.RegisterConnection(connection);

        try
        {
            // Handle Auto-Subscriptions
            foreach (var room in _options.AutoSubscribeRooms)
            {
                if (_options.CanJoinRoom != null)
                {
                    if (!await _options.CanJoinRoom(connection, room))
                    {
                        continue;
                    }
                }
                connection.JoinRoom(room);
            }

            foreach (var evt in _options.AutoSubscribeEvents)
            {
                connection.SubscribeToEvent(evt);
            }

            if (_options.OnConnected != null)
            {
                await _options.OnConnected(connection);
            }

            // Keep connection alive and handle incoming messages
            await ReceiveLoop(webSocket, connection, context);
        }
        finally
        {
            _registry.UnregisterConnection(connectionId);
            
            if (_options.OnDisconnected != null)
            {
                await _options.OnDisconnected(connectionId);
            }
        }
    }

    private async Task ReceiveLoop(WebSocket webSocket, WebSocketConnection connection, HttpContext context)
    {
        var buffer = new byte[1024 * 4];
        var handler = context.RequestServices.GetService<IRealtimeInputHandler>();

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                break;
            }
            
            if (result.MessageType == WebSocketMessageType.Text && handler != null)
            {
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                await handler.HandleMessageAsync(connection, message);
            }
        }
    }
}
