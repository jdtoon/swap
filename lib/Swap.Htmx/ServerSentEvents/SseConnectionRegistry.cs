using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// Interface for managing active SSE connections.
/// </summary>
public interface ISseConnectionRegistry
{
    /// <summary>
    /// Registers a new SSE connection.
    /// </summary>
    void RegisterConnection(SseConnection connection);

    /// <summary>
    /// Removes a connection from the registry.
    /// </summary>
    void UnregisterConnection(string connectionId);

    /// <summary>
    /// Broadcasts an event to all connections.
    /// </summary>
    Task BroadcastAsync(string eventName, string html, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to connections in specific rooms.
    /// </summary>
    Task BroadcastToRoomsAsync(string eventName, string html, IEnumerable<string> rooms, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to connections subscribed to specific events.
    /// </summary>
    Task BroadcastToSubscribersAsync(string eventName, string html, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to connections matching a filter predicate.
    /// </summary>
    Task BroadcastToFilteredAsync(string eventName, string html, Func<SseConnection, bool> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to authenticated users with specific roles.
    /// </summary>
    Task BroadcastToRolesAsync(string eventName, string html, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event to a specific user by user ID.
    /// </summary>
    Task BroadcastToUserAsync(string eventName, string html, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active connection IDs.
    /// </summary>
    IReadOnlyCollection<string> GetActiveConnectionIds();

    /// <summary>
    /// Gets connection count statistics.
    /// </summary>
    SseConnectionStats GetStats();
}

/// <summary>
/// Connection statistics for monitoring.
/// </summary>
public record SseConnectionStats(
    int TotalConnections,
    int ActiveConnections,
    IReadOnlyDictionary<string, int> ConnectionsByRoom,
    IReadOnlyDictionary<string, int> SubscriptionsByEvent
);

/// <summary>
/// Default implementation of SSE connection registry.
/// </summary>
internal sealed class SseConnectionRegistry : ISseConnectionRegistry
{
    private readonly ConcurrentDictionary<string, SseConnection> _connections;
    private readonly ISseBackplane _backplane;
    private readonly ILogger<SseConnectionRegistry> _logger;

    public SseConnectionRegistry(ISseBackplane backplane, ILogger<SseConnectionRegistry> logger)
    {
        _connections = new ConcurrentDictionary<string, SseConnection>();
        _backplane = backplane;
        _logger = logger;

        // Subscribe to backplane messages
        _ = _backplane.SubscribeAsync(HandleBackplaneMessageAsync);
    }

    private async Task HandleBackplaneMessageAsync(SseMessage message, CancellationToken ct)
    {
        switch (message.RecipientType)
        {
            case SseRecipientType.Broadcast:
                await SendToAllLocalAsync(message.EventName, message.Html);
                break;
            case SseRecipientType.Room:
                if (message.RecipientValues != null)
                {
                    await SendToRoomsLocalAsync(message.EventName, message.Html, message.RecipientValues);
                }
                break;
            case SseRecipientType.EventSubscription:
                await SendToSubscribersLocalAsync(message.EventName, message.Html);
                break;
            case SseRecipientType.Role:
                if (message.RecipientValues != null)
                {
                    await SendToRolesLocalAsync(message.EventName, message.Html, message.RecipientValues);
                }
                break;
            case SseRecipientType.User:
                if (message.RecipientValue != null)
                {
                    await SendToUserLocalAsync(message.EventName, message.Html, message.RecipientValue);
                }
                break;
        }
    }

    public void RegisterConnection(SseConnection connection)
    {
        _connections.TryAdd(connection.Id, connection);
        _logger.LogInformation("[SSE Registry] Connection registered: {ConnectionId} for user {UserId}. Total connections: {Total}",
            connection.Id, connection.User?.Identity?.Name ?? "anonymous", _connections.Count);
    }

    public void UnregisterConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            _logger.LogDebug("SSE connection unregistered: {ConnectionId}", connectionId);
            _ = Task.Run(async () => await connection.DisposeAsync());
        }
    }

    public async Task BroadcastAsync(string eventName, string html, CancellationToken cancellationToken = default)
    {
        await _backplane.PublishAsync(new SseMessage(eventName, html, SseRecipientType.Broadcast), cancellationToken);
    }

    private async Task SendToAllLocalAsync(string eventName, string html)
    {
        var activeConnections = GetActiveConnections();
        _logger.LogDebug("[SSE Registry] Local Broadcast - Event: {EventName}, Active connections: {Count}", 
            eventName, activeConnections.Count);
        
        if (activeConnections.Count == 0) return;
        
        var tasks = activeConnections.Select(conn => conn.SendEventAsync(eventName, html));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SSE Registry] Error during local SSE broadcast");
        }

        CleanupInactiveConnections();
    }

    public async Task BroadcastToRoomsAsync(string eventName, string html, IEnumerable<string> rooms, CancellationToken cancellationToken = default)
    {
        await _backplane.PublishAsync(new SseMessage(eventName, html, SseRecipientType.Room, RecipientValues: rooms.ToArray()), cancellationToken);
    }

    private async Task SendToRoomsLocalAsync(string eventName, string html, IEnumerable<string> rooms)
    {
        var roomSet = rooms.ToHashSet();
        var targetConnections = GetActiveConnections()
            .Where(conn => conn.Rooms.Any(room => roomSet.Contains(room)))
            .ToList();

        if (targetConnections.Count == 0) return;

        var tasks = targetConnections.Select(conn => conn.SendEventAsync(eventName, html));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during local SSE broadcast to rooms {Rooms}", string.Join(", ", rooms));
        }

        CleanupInactiveConnections();
    }

    public async Task BroadcastToSubscribersAsync(string eventName, string html, CancellationToken cancellationToken = default)
    {
        await _backplane.PublishAsync(new SseMessage(eventName, html, SseRecipientType.EventSubscription), cancellationToken);
    }

    private async Task SendToSubscribersLocalAsync(string eventName, string html)
    {
        var targetConnections = GetActiveConnections()
            .Where(conn => conn.IsSubscribedToEvent(eventName))
            .ToList();

        if (targetConnections.Count == 0) return;

        var tasks = targetConnections.Select(conn => conn.SendEventAsync(eventName, html));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during local SSE broadcast to event subscribers {EventName}", eventName);
        }

        CleanupInactiveConnections();
    }

    public async Task BroadcastToFilteredAsync(string eventName, string html, Func<SseConnection, bool> filter, CancellationToken cancellationToken = default)
    {
        var targetConnections = GetActiveConnections()
            .Where(filter)
            .ToList();

        var tasks = targetConnections.Select(conn => conn.SendEventAsync(eventName, html));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during SSE filtered broadcast for event {EventName}", eventName);
        }

        CleanupInactiveConnections();
    }

    public async Task BroadcastToRolesAsync(string eventName, string html, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        await _backplane.PublishAsync(new SseMessage(eventName, html, SseRecipientType.Role, RecipientValues: roles.ToArray()), cancellationToken);
    }

    private async Task SendToRolesLocalAsync(string eventName, string html, IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet();
        var targetConnections = GetActiveConnections()
            .Where(conn => conn.User?.Identity?.IsAuthenticated == true &&
                          roleSet.Any(role => conn.User.IsInRole(role)))
            .ToList();

        if (targetConnections.Count == 0) return;

        var tasks = targetConnections.Select(conn => conn.SendEventAsync(eventName, html));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during local SSE broadcast to roles {Roles}", string.Join(", ", roles));
        }

        CleanupInactiveConnections();
    }

    public async Task BroadcastToUserAsync(string eventName, string html, string userId, CancellationToken cancellationToken = default)
    {
        await _backplane.PublishAsync(new SseMessage(eventName, html, SseRecipientType.User, RecipientValue: userId), cancellationToken);
    }

    private async Task SendToUserLocalAsync(string eventName, string html, string userId)
    {
        var targetConnections = GetActiveConnections()
            .Where(conn =>
            {
                if (conn.User?.Identity?.IsAuthenticated != true) return false;

                var userIdClaim = conn.User.FindFirst("sub")?.Value
                               ?? conn.User.FindFirst("id")?.Value
                               ?? conn.User.Identity.Name;

                return string.Equals(userIdClaim, userId, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (targetConnections.Count == 0) return;

        var tasks = targetConnections.Select(conn => conn.SendEventAsync(eventName, html));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during local SSE broadcast to user {UserId}", userId);
        }

        CleanupInactiveConnections();
    }

    public IReadOnlyCollection<string> GetActiveConnectionIds()
    {
        return GetActiveConnections().Select(c => c.Id).ToList();
    }

    public SseConnectionStats GetStats()
    {
        var activeConnections = GetActiveConnections();

        var connectionsByRoom = activeConnections
            .SelectMany(conn => conn.Rooms, (conn, room) => room)
            .GroupBy(room => room)
            .ToDictionary(g => g.Key, g => g.Count());

        var subscriptionsByEvent = activeConnections
            .SelectMany(conn => conn.SubscribedEvents, (conn, eventName) => eventName)
            .GroupBy(eventName => eventName)
            .ToDictionary(g => g.Key, g => g.Count());

        return new SseConnectionStats(
            TotalConnections: _connections.Count,
            ActiveConnections: activeConnections.Count,
            ConnectionsByRoom: connectionsByRoom,
            SubscriptionsByEvent: subscriptionsByEvent
        );
    }

    private List<SseConnection> GetActiveConnections()
    {
        return _connections.Values.Where(conn => conn.IsActive).ToList();
    }

    private void CleanupInactiveConnections()
    {
        var inactiveConnections = _connections.Values.Where(conn => !conn.IsActive).ToList();

        foreach (var connection in inactiveConnections)
        {
            UnregisterConnection(connection.Id);
        }

        if (inactiveConnections.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} inactive SSE connections", inactiveConnections.Count);
        }
    }
}