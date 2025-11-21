namespace Swap.Htmx.Realtime;

/// <summary>
/// Interface for managing active realtime connections (SSE, WebSocket, etc.).
/// </summary>
public interface IRealtimeConnectionRegistry
{
    /// <summary>
    /// Registers a new connection.
    /// </summary>
    void RegisterConnection(IRealtimeConnection connection);

    /// <summary>
    /// Removes a connection from the registry.
    /// </summary>
    void UnregisterConnection(string connectionId);

    /// <summary>
    /// Broadcasts an event to all connections.
    /// </summary>
    Task BroadcastAsync(string eventName, string data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to connections in specific rooms.
    /// </summary>
    Task BroadcastToRoomsAsync(string eventName, string data, IEnumerable<string> rooms, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to connections subscribed to specific events.
    /// </summary>
    Task BroadcastToSubscribersAsync(string eventName, string data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to connections matching a filter predicate.
    /// </summary>
    Task BroadcastToFilteredAsync(string eventName, string data, Func<IRealtimeConnection, bool> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to authenticated users with specific roles.
    /// </summary>
    Task BroadcastToRolesAsync(string eventName, string data, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to a specific user by user ID.
    /// </summary>
    Task BroadcastToUserAsync(string eventName, string data, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active connection IDs.
    /// </summary>
    IReadOnlyCollection<string> GetActiveConnectionIds();

    /// <summary>
    /// Gets connection count statistics.
    /// </summary>
    RealtimeConnectionStats GetStats();
}

/// <summary>
/// Connection statistics for monitoring.
/// </summary>
public record RealtimeConnectionStats(
    int TotalConnections,
    int ActiveConnections,
    IReadOnlyDictionary<string, int> ConnectionsByRoom,
    IReadOnlyDictionary<string, int> SubscriptionsByEvent
);
