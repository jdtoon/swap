namespace Swap.Htmx.Realtime;

/// <summary>
/// Represents a single connection's presence within a room.
/// </summary>
/// <param name="ConnectionId">The realtime connection identifier.</param>
/// <param name="UserId">The associated user identifier, if any.</param>
/// <param name="Room">The room the connection is present in.</param>
public record PresenceEntry(string ConnectionId, string? UserId, string Room);

/// <summary>
/// Tracks which connections (and their associated users) are present in which rooms.
/// This is a single-node, in-process abstraction with no cross-node backplane support.
/// </summary>
public interface IRealtimePresence
{
    /// <summary>
    /// Marks a connection as present in a room.
    /// </summary>
    void Track(string connectionId, string room, string? userId);

    /// <summary>
    /// Removes a connection's presence from a specific room.
    /// </summary>
    void Untrack(string connectionId, string room);

    /// <summary>
    /// Removes a connection's presence from all rooms it was tracked in.
    /// </summary>
    void UntrackAll(string connectionId);

    /// <summary>
    /// Gets the distinct set of presence entries currently tracked for a room.
    /// </summary>
    IReadOnlyList<PresenceEntry> List(string room);

    /// <summary>
    /// Gets the number of distinct connections currently tracked in a room.
    /// </summary>
    int Count(string room);
}
