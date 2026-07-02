using System.Collections.Concurrent;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Thread-safe, single-node, in-memory implementation of <see cref="IRealtimePresence"/>.
/// State is held in-process only; it does not synchronize across nodes (no backplane).
/// </summary>
/// <remarks>
/// Uses a single source of truth (room → connection → user) so there is no reverse index to fall out
/// of sync. Emptied room buckets are intentionally left in place rather than eagerly removed: removing
/// a container that a concurrent <see cref="Track"/> may be repopulating is a lost-update race, and the
/// number of distinct rooms is bounded in practice. This trades a little idle memory for correctness.
/// </remarks>
public sealed class InMemoryRealtimePresence : IRealtimePresence
{
    // room -> (connectionId -> userId). The only state; no reverse index to desynchronize.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string?>> _rooms = new();

    /// <inheritdoc />
    public void Track(string connectionId, string room, string? userId)
    {
        if (string.IsNullOrWhiteSpace(connectionId) || string.IsNullOrWhiteSpace(room))
        {
            return;
        }

        var connections = _rooms.GetOrAdd(room, static _ => new ConcurrentDictionary<string, string?>());
        connections[connectionId] = userId;
    }

    /// <inheritdoc />
    public void Untrack(string connectionId, string room)
    {
        if (string.IsNullOrWhiteSpace(connectionId) || string.IsNullOrWhiteSpace(room))
        {
            return;
        }

        if (_rooms.TryGetValue(room, out var connections))
        {
            connections.TryRemove(connectionId, out _);
        }
    }

    /// <inheritdoc />
    public void UntrackAll(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        // Single source of truth: sweep every room. Rooms are bounded, and this can never orphan the
        // connection (there is no separate index that could miss a room a concurrent Track just added).
        foreach (var connections in _rooms.Values)
        {
            connections.TryRemove(connectionId, out _);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PresenceEntry> List(string room)
    {
        if (string.IsNullOrWhiteSpace(room) || !_rooms.TryGetValue(room, out var connections))
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
        if (string.IsNullOrWhiteSpace(room) || !_rooms.TryGetValue(room, out var connections))
        {
            return 0;
        }

        return connections.Count;
    }
}
