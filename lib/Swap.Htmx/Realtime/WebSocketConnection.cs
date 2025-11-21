using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Represents an active WebSocket connection.
/// </summary>
public sealed class WebSocketConnection : IRealtimeConnection
{
    private readonly WebSocket _webSocket;
    private readonly CancellationTokenSource _cts;
    private readonly ConcurrentDictionary<string, bool> _subscribedEvents = new();
    private readonly ConcurrentDictionary<string, bool> _joinedRooms = new();
    private bool _disposed;

    public string Id { get; }
    public ClaimsPrincipal? User { get; }
    public DateTime ConnectedAt { get; }
    public bool IsActive => !_disposed && _webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested;
    public IReadOnlyCollection<string> Rooms => _joinedRooms.Keys.ToList();
    public IReadOnlyCollection<string> SubscribedEvents => _subscribedEvents.Keys.ToList();

    public WebSocketConnection(string id, WebSocket webSocket, HttpContext httpContext)
    {
        Id = id;
        _webSocket = webSocket;
        User = httpContext.User;
        ConnectedAt = DateTime.UtcNow;
        _cts = new CancellationTokenSource();
    }

    public async Task SendEventAsync(string eventName, string data)
    {
        if (!IsActive) return;

        try
        {
            // For HTMX WebSockets, we send the HTML payload directly.
            // The eventName is used for server-side routing/filtering.
            // If the payload contains OOB swaps, HTMX will handle them.
            var bytes = Encoding.UTF8.GetBytes(data);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }
        catch (Exception)
        {
            // Connection likely dead
            _cts.Cancel();
        }
    }

    public void JoinRoom(string room)
    {
        if (!string.IsNullOrWhiteSpace(room)) _joinedRooms.TryAdd(room, true);
    }

    public void LeaveRoom(string room)
    {
        if (!string.IsNullOrWhiteSpace(room)) _joinedRooms.TryRemove(room, out _);
    }

    public bool IsInRoom(string room) => _joinedRooms.ContainsKey(room);

    public void SubscribeToEvent(string eventName)
    {
        if (!string.IsNullOrWhiteSpace(eventName)) _subscribedEvents.TryAdd(eventName, true);
    }

    public void UnsubscribeFromEvent(string eventName)
    {
        if (!string.IsNullOrWhiteSpace(eventName)) _subscribedEvents.TryRemove(eventName, out _);
    }

    public bool IsSubscribedToEvent(string eventName) => _subscribedEvents.ContainsKey(eventName);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        
        try 
        {
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            _webSocket.Dispose();
        }
        catch 
        {
            // Ignore disposal errors
        }
        
        _cts.Dispose();
    }
}
