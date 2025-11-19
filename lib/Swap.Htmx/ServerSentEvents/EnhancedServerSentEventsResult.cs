using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// Enhanced SSE result that integrates with the connection registry and event bridge.
/// </summary>
public sealed class EnhancedServerSentEventsResult : Microsoft.AspNetCore.Mvc.IActionResult
{
    private readonly Func<SseConnectionBuilder, CancellationToken, Task> _handler;

    public EnhancedServerSentEventsResult(Func<SseConnectionBuilder, CancellationToken, Task> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
    {
        var response = context.HttpContext.Response;

        // Set SSE headers per W3C EventSource specification
        response.StatusCode = 200;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no"; // Prevent Nginx buffering

        // Disable response buffering for real-time streaming
        var bufferingFeature = context.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        var cancellationToken = context.HttpContext.RequestAborted;
        var stream = new ServerSentEventStream(response, cancellationToken);
        var connectionId = Guid.NewGuid().ToString("N");
        var connection = new SseConnection(connectionId, stream, context.HttpContext);

        // Register with connection registry if available
        var registry = context.HttpContext.RequestServices.GetService<ISseConnectionRegistry>();
        registry?.RegisterConnection(connection);

        var builder = new SseConnectionBuilder(connection);

        try
        {
            await _handler(builder, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is normal for SSE
        }
        finally
        {
            registry?.UnregisterConnection(connectionId);
            await connection.DisposeAsync();
        }
    }
}

/// <summary>
/// Fluent builder for configuring SSE connections.
/// </summary>
public sealed class SseConnectionBuilder
{
    private readonly SseConnection _connection;

    internal SseConnectionBuilder(SseConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// The underlying SSE connection for direct access.
    /// </summary>
    public SseConnection Connection => _connection;

    /// <summary>
    /// Joins one or more rooms for targeted broadcasting.
    /// </summary>
    public SseConnectionBuilder WithRooms(params string[] rooms)
    {
        foreach (var room in rooms)
        {
            _connection.JoinRoom(room);
        }
        return this;
    }

    /// <summary>
    /// Joins a user-specific room based on the authenticated user's ID.
    /// Requires authentication.
    /// </summary>
    public SseConnectionBuilder WithUserRoom()
    {
        WithAuthentication();
        var userId = _connection.User!.FindFirst("sub")?.Value
                  ?? _connection.User!.FindFirst("id")?.Value
                  ?? _connection.User!.Identity!.Name;

        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("Could not determine user ID for room assignment");
        }

        _connection.JoinRoom($"user-{userId}");
        return this;
    }

    /// <summary>
    /// Joins rooms based on user roles.
    /// Requires authentication.
    /// </summary>
    public SseConnectionBuilder WithRoleRooms()
    {
        WithAuthentication();
        var roles = _connection.User!.Claims
            .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value);

        foreach (var role in roles)
        {
            _connection.JoinRoom($"role-{role}");
        }

        return this;
    }

    /// <summary>
    /// Subscribes to specific events.
    /// </summary>
    public SseConnectionBuilder WithEvents(params string[] events)
    {
        foreach (var eventName in events)
        {
            _connection.SubscribeToEvent(eventName);
        }
        return this;
    }

    /// <summary>
    /// Subscribes to events matching a pattern.
    /// Supports wildcards: * (any characters), ? (single character).
    /// </summary>
    public SseConnectionBuilder WithEventPattern(string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        _connection.SubscribeToEvent($"pattern:{regexPattern}");
        return this;
    }

    /// <summary>
    /// Subscribes to all events with a specific prefix.
    /// </summary>
    public SseConnectionBuilder WithEventPrefix(string prefix)
    {
        _connection.SubscribeToEvent($"prefix:{prefix}");
        return this;
    }

    /// <summary>
    /// Subscribes to events based on user permissions.
    /// Only receives events the authenticated user is authorized to see.
    /// </summary>
    public SseConnectionBuilder WithAuthorizedEvents()
    {
        WithAuthentication();
        _connection.SubscribeToEvent("authorized-only");
        return this;
    }

    /// <summary>
    /// Requires authentication for this connection.
    /// Throws if user is not authenticated.
    /// </summary>
    public SseConnectionBuilder WithAuthentication()
    {
        if (_connection.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("SSE connection requires authentication");
        }
        return this;
    }

    /// <summary>
    /// Requires authentication and specific roles for this connection.
    /// Throws if user is not authenticated or doesn't have required roles.
    /// </summary>
    public SseConnectionBuilder WithAuthentication(params string[] requiredRoles)
    {
        if (_connection.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("SSE connection requires authentication");
        }

        if (requiredRoles.Length > 0)
        {
            var hasAnyRole = requiredRoles.Any(role => _connection.User.IsInRole(role));
            if (!hasAnyRole)
            {
                throw new UnauthorizedAccessException($"SSE connection requires one of the following roles: {string.Join(", ", requiredRoles)}");
            }
        }

        return this;
    }

    /// <summary>
    /// Requires specific claims for this connection.
    /// Throws if user doesn't have required claims.
    /// </summary>
    public SseConnectionBuilder WithClaims(params (string type, string value)[] requiredClaims)
    {
        WithAuthentication();

        foreach (var (claimType, claimValue) in requiredClaims)
        {
            if (!_connection.User!.HasClaim(claimType, claimValue))
            {
                throw new UnauthorizedAccessException($"SSE connection requires claim '{claimType}' with value '{claimValue}'");
            }
        }

        return this;
    }

    /// <summary>
    /// Sends initial state to the connection.
    /// </summary>
    public async Task<SseConnectionBuilder> WithInitialState(string eventName, string html)
    {
        await _connection.SendEventAsync(eventName, html);
        return this;
    }

    /// <summary>
    /// Sends initial state using a factory function.
    /// </summary>
    public async Task<SseConnectionBuilder> WithInitialState(string eventName, Func<Task<string>> htmlFactory)
    {
        var html = await htmlFactory();
        await _connection.SendEventAsync(eventName, html);
        return this;
    }

    /// <summary>
    /// Keeps the connection alive with periodic heartbeats.
    /// </summary>
    public async Task KeepAlive(TimeSpan? interval = null, CancellationToken cancellationToken = default)
    {
        var heartbeatInterval = interval ?? TimeSpan.FromSeconds(30);

        while (!cancellationToken.IsCancellationRequested && _connection.IsActive)
        {
            try
            {
                await Task.Delay(heartbeatInterval, cancellationToken);
                if (_connection.IsActive)
                {
                    await _connection.Stream.SendKeepAliveAsync();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Connection lost, exit gracefully
                break;
            }
        }
    }

    /// <summary>
    /// Configures connection timeout and automatic reconnection hints.
    /// </summary>
    public SseConnectionBuilder WithTimeout(TimeSpan timeout, bool sendReconnectHint = true)
    {
        // Store timeout info for potential client-side use
        _connection.JoinRoom($"timeout-{timeout.TotalSeconds}s");

        if (sendReconnectHint)
        {
            // Send initial retry hint to client
            Task.Run(async () =>
            {
                await _connection.SendEventAsync("connection-config",
                    $"{{\"timeout\": {timeout.TotalSeconds}, \"retry\": {Math.Min(timeout.TotalSeconds / 2, 10)}}}");
            });
        }

        return this;
    }

    /// <summary>
    /// Enables connection monitoring and statistics.
    /// </summary>
    public SseConnectionBuilder WithMonitoring(bool includeMetrics = true)
    {
        if (includeMetrics)
        {
            _connection.SubscribeToEvent("connection-stats");
            _connection.JoinRoom("monitoring");

            // Send initial connection info
            Task.Run(async () =>
            {
                var stats = new
                {
                    connectionId = _connection.Id,
                    connectedAt = _connection.ConnectedAt,
                    userId = _connection.User?.Identity?.Name ?? "anonymous",
                    rooms = _connection.Rooms,
                    events = _connection.SubscribedEvents
                };

                var json = System.Text.Json.JsonSerializer.Serialize(stats);
                await _connection.SendEventAsync("connection-info",
                    $"<div data-connection-info=\"{System.Web.HttpUtility.HtmlEncode(json)}\"></div>");
            });
        }

        return this;
    }

    /// <summary>
    /// Adds custom metadata to the connection for filtering and routing.
    /// </summary>
    public SseConnectionBuilder WithMetadata(string key, string value)
    {
        // Use rooms as a way to store metadata for now
        _connection.JoinRoom($"meta:{key}:{value}");
        return this;
    }

    /// <summary>
    /// Configures the connection with multiple metadata key-value pairs.
    /// </summary>
    public SseConnectionBuilder WithMetadata(IDictionary<string, string> metadata)
    {
        foreach (var kvp in metadata)
        {
            WithMetadata(kvp.Key, kvp.Value);
        }
        return this;
    }

    /// <summary>
    /// Enables debug mode for this connection with detailed logging.
    /// </summary>
    public SseConnectionBuilder WithDebugMode()
    {
        _connection.JoinRoom("debug");
        _connection.SubscribeToEvent("debug-info");
        return this;
    }
}