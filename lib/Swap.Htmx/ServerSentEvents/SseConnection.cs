using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// Represents an active SSE connection with user context and room subscriptions.
/// </summary>
public sealed class SseConnection : IAsyncDisposable
{
    private readonly ServerSentEventStream _stream;
    private readonly CancellationTokenSource _cts;
    private readonly HttpContext _httpContext;
    private readonly ConcurrentDictionary<string, bool> _subscribedEvents;
    private readonly ConcurrentDictionary<string, bool> _joinedRooms;
    private bool _disposed;

    public string Id { get; }
    public ClaimsPrincipal? User { get; }
    public DateTime ConnectedAt { get; }

    /// <summary>
    /// Internal access to the SSE stream for advanced scenarios.
    /// </summary>
    internal ServerSentEventStream Stream => _stream;

    internal SseConnection(string id, ServerSentEventStream stream, HttpContext httpContext)
    {
        Id = id;
        _stream = stream;
        _httpContext = httpContext;
        _cts = new CancellationTokenSource();
        _subscribedEvents = new ConcurrentDictionary<string, bool>();
        _joinedRooms = new ConcurrentDictionary<string, bool>();
        User = httpContext.User;
        ConnectedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sends an SSE event to this specific connection.
    /// </summary>
    public async Task SendEventAsync(string eventName, string html)
    {
        if (_disposed || _cts.Token.IsCancellationRequested) return;

        try
        {
            await _stream.SendEventAsync(eventName, html);
        }
        catch (Exception)
        {
            // Connection lost, mark for cleanup
            _cts.Cancel();
        }
    }

    /// <summary>
    /// Joins a room for targeted broadcasting.
    /// </summary>
    public void JoinRoom(string room)
    {
        if (string.IsNullOrWhiteSpace(room)) return;
        _joinedRooms.TryAdd(room, true);
    }

    /// <summary>
    /// Leaves a room.
    /// </summary>
    public void LeaveRoom(string room)
    {
        if (string.IsNullOrWhiteSpace(room)) return;
        _joinedRooms.TryRemove(room, out _);
    }

    /// <summary>
    /// Subscribes to specific events.
    /// </summary>
    public void SubscribeToEvent(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        _subscribedEvents.TryAdd(eventName, true);
    }

    /// <summary>
    /// Unsubscribes from specific events.
    /// </summary>
    public void UnsubscribeFromEvent(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        _subscribedEvents.TryRemove(eventName, out _);
    }

    /// <summary>
    /// Checks if connection is subscribed to an event.
    /// </summary>
    public bool IsSubscribedToEvent(string eventName)
    {
        return _subscribedEvents.ContainsKey(eventName);
    }

    /// <summary>
    /// Checks if connection is in a specific room.
    /// </summary>
    public bool IsInRoom(string room)
    {
        return _joinedRooms.ContainsKey(room);
    }

    /// <summary>
    /// Gets all rooms this connection has joined.
    /// </summary>
    public IReadOnlyCollection<string> Rooms => _joinedRooms.Keys.ToList();

    /// <summary>
    /// Gets all events this connection is subscribed to.
    /// </summary>
    public IReadOnlyCollection<string> SubscribedEvents => _subscribedEvents.Keys.ToList();

    /// <summary>
    /// Checks if the connection is still active.
    /// </summary>
    public bool IsActive => !_disposed && !_cts.Token.IsCancellationRequested;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        await _stream.DisposeAsync();
        _cts.Dispose();
    }
}