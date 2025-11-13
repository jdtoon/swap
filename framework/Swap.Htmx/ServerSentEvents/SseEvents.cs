using Swap.Htmx.Events;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// Static helper class for creating SSE-related event keys.
/// Used for creating event chains that trigger SSE broadcasts.
/// </summary>
public static class SseEvents
{
    /// <summary>
    /// Creates an event key for broadcasting to all SSE connections.
    /// </summary>
    /// <param name="eventName">The SSE event name that will be sent to clients.</param>
    public static EventKey Broadcast(string eventName) => new($"sse:broadcast:{eventName}");

    /// <summary>
    /// Creates an event key for broadcasting to specific rooms.
    /// </summary>
    /// <param name="room">The room to broadcast to.</param>
    /// <param name="eventName">The SSE event name that will be sent to clients.</param>
    public static EventKey Room(string room, string eventName) => new($"sse:room:{room}:{eventName}");

    /// <summary>
    /// Creates an event key for broadcasting to event subscribers.
    /// </summary>
    /// <param name="eventName">The SSE event name that will be sent to clients.</param>
    public static EventKey Subscribers(string eventName) => new($"sse:subscribers:{eventName}");

    /// <summary>
    /// Creates a fluent room builder for multiple rooms.
    /// </summary>
    /// <param name="rooms">The rooms to broadcast to.</param>
    public static SseRoomBuilder Rooms(params string[] rooms) => new(rooms);

    // Common SSE event names for convenience
    public static class Common
    {
        public static EventKey Refresh => Broadcast("refresh");
        public static EventKey Update => Broadcast("update");
        public static EventKey Notification => Broadcast("notification");
        public static EventKey Toast => Broadcast("toast");
        public static EventKey Metrics => Broadcast("metrics");
        public static EventKey Activity => Broadcast("activity");
    }
}

/// <summary>
/// Fluent builder for creating room-specific SSE events.
/// </summary>
public sealed class SseRoomBuilder
{
    private readonly string[] _rooms;

    internal SseRoomBuilder(string[] rooms)
    {
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
    }

    /// <summary>
    /// Creates an event key for broadcasting to the specified rooms.
    /// </summary>
    /// <param name="eventName">The SSE event name that will be sent to clients.</param>
    public EventKey Send(string eventName)
    {
        var roomList = string.Join(",", _rooms);
        return new EventKey($"sse:rooms:{roomList}:{eventName}");
    }
}