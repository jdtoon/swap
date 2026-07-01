using System.Collections.Concurrent;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Thread-safe, single-node, in-memory implementation of <see cref="IRealtimePresence"/>.
/// State is held in-process only; it does not synchronize across nodes (no backplane).
/// </summary>
public sealed class InMemoryRealtimePresence : IRealtimePresence
{
    // room -> (connectionId -> userId)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string?>> _roomConnections = new();

    // connectionId -> set of rooms it has been tracked in (used by UntrackAll for cleanup).
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connectionRooms = new();

    /// <inheritdoc />
    public void Track(string connectionId, string room, string? userId)
    {
        if (string.IsNullOrWhiteSpace(connectionId) || string.IsNullOrWhiteSpace(room)) return;

        var connections = _roomConnections.GetOrAdd(room, static _ => new ConcurrentDictionary<string, string?>());
        connections[connectionId] = userId;

        var rooms = _connectionRooms.GetOrAdd(connectionId, static _ => new ConcurrentDictionary<string, byte>());
        rooms[room] = 0;
    }

    /// <inheritdoc />
    public void Untrack(string connectionId, string room)
    {
        if (string.IsNullOrWhiteSpace(connectionId) || string.IsNullOrWhiteSpace(room)) return;

        RemoveFromRoom(connectionId, room);

        if (_connectionRooms.TryGetValue(connectionId, out var rooms))
        {
            rooms.TryRemove(room, out _);
            if (rooms.IsEmpty)
            {
                _connectionRooms.TryRemove(connectionId, out _);
            }
        }
    }

    /// <inheritdoc />
    public void UntrackAll(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) return;

        if (_connectionRooms.TryRemove(connectionId, out var rooms))
        {
            foreach (var room in rooms.Keys)
            {
                RemoveFromRoom(connectionId, room);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PresenceEntry> List(string room)
    {
        if (string.IsNullOrWhiteSpace(room) || !_roomConnections.TryGetValue(room, out var connections))
        {
            return Array.Empty<PresenceEntry>();
        }

        return connections
            .Select(pair => new PresenceEntry(pair.Key, pair.Value, room))
            .ToList();
    }

    /// <inheritdoc />
    public int Count(string room)
    {
        if (string.IsNullOrWhiteSpace(room) || !_roomConnections.TryGetValue(room, out var connections))
        {
            return 0;
        }

        return connections.Count;
    }

    private void RemoveFromRoom(string connectionId, string room)
    {
        if (!_roomConnections.TryGetValue(room, out var connections))
        {
            return;
        }

        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
        {
            // Best-effort cleanup: only remove if still empty at removal time.
            _roomConnections.TryRemove(room, out var removed);
            if (removed != null && !removed.IsEmpty)
            {
                // Someone re-added a connection concurrently; put it back.
                _roomConnections.TryAdd(room, removed);
            }
        }
    }
}
