using System.Security.Claims;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Represents an abstract realtime connection (SSE, WebSocket, etc.).
/// </summary>
public interface IRealtimeConnection : IAsyncDisposable
{
    /// <summary>
    /// Unique identifier for the connection.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The user associated with the connection.
    /// </summary>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// When the connection was established.
    /// </summary>
    DateTime ConnectedAt { get; }

    /// <summary>
    /// Whether the connection is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// List of rooms this connection has joined.
    /// </summary>
    IReadOnlyCollection<string> Rooms { get; }

    /// <summary>
    /// List of events this connection is explicitly subscribed to.
    /// </summary>
    IReadOnlyCollection<string> SubscribedEvents { get; }

    /// <summary>
    /// Sends a message/event to the client.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="data">The data payload (usually HTML for HTMX, or JSON).</param>
    Task SendEventAsync(string eventName, string data);

    /// <summary>
    /// Joins a room for targeted broadcasting.
    /// </summary>
    void JoinRoom(string room);

    /// <summary>
    /// Leaves a room.
    /// </summary>
    void LeaveRoom(string room);

    /// <summary>
    /// Checks if connection is in a specific room.
    /// </summary>
    bool IsInRoom(string room);

    /// <summary>
    /// Subscribes to specific events.
    /// </summary>
    void SubscribeToEvent(string eventName);

    /// <summary>
    /// Unsubscribes from specific events.
    /// </summary>
    void UnsubscribeFromEvent(string eventName);

    /// <summary>
    /// Checks if connection is subscribed to an event.
    /// </summary>
    bool IsSubscribedToEvent(string eventName);
}
