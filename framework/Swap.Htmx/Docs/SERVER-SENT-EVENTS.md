# Enhanced Server-Sent Events (SSE)

Real-time HTML streaming with automatic fallback, connection management, and cross-module broadcasting.

## Overview

Swap.Htmx provides advanced Server-Sent Events support with enterprise-grade features:

- ✅ **Automatic Fallback** - Seamless HTTP polling when SSE fails
- ✅ **Connection Management** - Room-based broadcasting with authentication
- ✅ **Event Bridge** - Automatic domain event → SSE broadcasting
- ✅ **Performance** - Cached polling endpoints and connection pooling
- ✅ **Monitoring** - Connection status tracking and debugging tools

Perfect for:

- Real-time dashboards with reliable updates
- Live notifications that work on all networks
- Cross-module communication in modular monoliths
- Collaborative features with presence tracking
- Production systems requiring 99.9% uptime

## Quick Start

### 1. Register Enhanced SSE Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Enhanced SSE services with connection management
builder.Services.AddSseEventBridge();
builder.Services.AddSseFallback(options => {
    options.DefaultPollInterval = 5000;  // 5 second polling fallback
    options.MaxSseRetries = 3;           // Retry SSE 3 times before fallback
    options.EnableFallback = true;       // Enable automatic fallback
    options.FallbackAfterErrors = 2;     // Switch to polling after 2 errors
});

// Add memory cache for fallback optimization
builder.Services.AddMemoryCache();

var app = builder.Build();

// Add SSE middleware to pipeline
app.UseSwapHtmxShell();
app.UseSwapHtmx();
app.UseSseEventBridge();  // ← New enhanced SSE middleware

app.Run();
```

### 2. Enhanced SSE Controller

```csharp
public class DashboardController : SwapController
{
    private readonly ISseConnectionRegistry _connectionRegistry;
    private readonly ISseFallbackService _fallbackService;

    public DashboardController(
        ISseConnectionRegistry connectionRegistry,
        ISseFallbackService fallbackService)
    {
        _connectionRegistry = connectionRegistry;
        _fallbackService = fallbackService;
    }

    [HttpGet("/dashboard/live")]
    public IActionResult LiveMetrics()
    {
        // Check if client should use polling fallback
        if (_fallbackService.ShouldUsePolling(HttpContext))
        {
            return this.CachedPollingFallback(
                cacheKey: "dashboard-metrics",
                getContentFunc: async () => {
                    var data = await GetDashboardDataAsync();
                    return await this.RenderPartialToStringAsync("_Metrics", data);
                },
                cacheDuration: TimeSpan.FromSeconds(10)
            );
        }

        // Enhanced SSE with connection management
        return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) => {
            // Configure connection with rooms and event filters
            connectionBuilder.WithRooms("dashboard", "metrics")
                           .WithEventPrefix("ui.dashboard")
                           .WithAuthentication(); // Requires authenticated user

            var connection = connectionBuilder.Connection;

            // Send initial state
            var initialData = await GetDashboardDataAsync();
            var initialHtml = await this.RenderPartialToStringAsync("_Metrics", initialData);
            await connection.SendEventAsync("metrics-update", initialHtml);

            // Keep connection alive - framework handles automatic updates via event bridge
            await connectionBuilder.KeepAlive(TimeSpan.FromSeconds(30), cancellationToken);
        });
    }

    // Manual broadcasting example
    [HttpPost("/dashboard/broadcast")]
    public async Task<IActionResult> BroadcastAlert([FromForm] string message)
    {
        var alertHtml = $@"
            <div class=""alert alert-info"">
                <strong>System Alert:</strong> {message}
                <small>({DateTime.Now:HH:mm:ss})</small>
            </div>";

        // Broadcast to all users in 'dashboard' room
        await _connectionRegistry.BroadcastToRoomsAsync("alert", alertHtml, new[] { "dashboard" });

        return SwapView("BroadcastSuccess");
    }
}
```

### 3. Client-Side with Automatic Fallback

```html
<!-- Enhanced SSE with automatic fallback -->
<div
  id="live-dashboard"
  hx-ext="sse-fallback"
  hx-sse="connect:/dashboard/live"
  hx-sse-swap="metrics-update"
  hx-sse-fallback-url="/dashboard/live"
  hx-sse-fallback-interval="5000"
>
  <!-- Connection status indicator -->
  <div class="connection-status">
    <span id="connection-indicator">Connecting...</span>
  </div>

  <!-- Live content area -->
  <div id="dashboard-content">Loading dashboard...</div>
</div>

<!-- Include SSE fallback JavaScript -->
<script src="/js/sse-fallback.js"></script>

<script>
  // Monitor connection status
  document.addEventListener("sse:connected", (e) => {
    document.getElementById("connection-indicator").innerHTML =
      '<span class="badge badge-success">🟢 Live (SSE)</span>';
    console.log("SSE connected:", e.detail.elementId);
  });

  document.addEventListener("sse:fallback", (e) => {
    document.getElementById("connection-indicator").innerHTML =
      '<span class="badge badge-warning">🟡 Live (Polling)</span>';
    console.log("Switched to polling fallback");
  });

  document.addEventListener("sse:error", (e) => {
    document.getElementById("connection-indicator").innerHTML =
      '<span class="badge badge-danger">🔴 Disconnected</span>';
    console.log("Connection error:", e.detail.error);
  });
</script>
```

## Core Concepts

### 1. Enhanced Connection Management

The `EnhancedServerSentEventsResult` provides advanced connection management:

```csharp
return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) => {
    // Room-based broadcasting
    connectionBuilder.WithRooms("admin", "dashboard")

    // Authentication requirements
    .WithAuthentication("Admin", "Manager")  // Requires specific roles

    // Event filtering
    .WithEventPrefix("ui.admin")             // Only admin UI events
    .WithEventPattern("*.created")           // All creation events

    // User-specific rooms
    .WithUserRoom()                          // Auto-join user-specific room
    .WithRoleRooms();                        // Auto-join role-based rooms

    var connection = connectionBuilder.Connection;

    // Send initial state
    await connection.SendEventAsync("welcome", "Connected to admin dashboard");

    // Framework handles the rest via event bridge
    await connectionBuilder.KeepAlive(TimeSpan.FromSeconds(30), cancellationToken);
});
```

### 2. Automatic Event Bridge

Domain events automatically trigger SSE broadcasts when configured:

```csharp
// 1. Define SSE-specific events in your modules
public static class ProjectSseEvents
{
    public static readonly EventKey StatusChanged = new("sse.project.status");
    public static readonly EventKey Created = new("sse.project.created");
}

// 2. Configure event chains (in Program.cs or module configuration)
builder.Services.AddSwapHtmx(events => {
    // When domain event occurs, trigger SSE broadcast
    events.Chain(ProjectEvents.StatusChanged, ProjectSseEvents.StatusChanged)
          .Chain(ProjectEvents.Created, ProjectSseEvents.Created);
});

// 3. Create event handler for automatic SSE broadcasting
public class ProjectSseHandler
{
    private readonly ISseConnectionRegistry _connectionRegistry;

    public async Task HandleAsync(EventKey eventKey, object payload)
    {
        if (eventKey == ProjectSseEvents.StatusChanged)
        {
            var html = await RenderProjectStatusUpdate(payload);
            await _connectionRegistry.BroadcastToRoomsAsync("project-update", html, new[] { "projects" });
        }
    }
}

// 4. In your domain service, emit events as usual
public async Task UpdateProjectStatus(int id, string status)
{
    await _repository.UpdateStatusAsync(id, status);

    // This automatically triggers SSE broadcast via event chain
    await _eventBus.EmitAsync(ProjectEvents.StatusChanged, new { Id = id, Status = status });
}
```

### 3. Fallback Polling

When SSE connections fail, the system automatically falls back to HTTP polling:

```csharp
// Polling endpoint that works with SSE fallback
[HttpGet("/dashboard/metrics")]
public async Task<IActionResult> MetricsPolling()
{
    // Use fallback extension for optimal performance
    return await this.PollingFallback(async lastEventId => {
        // Only return new data if there are updates
        if (HasUpdatesAfter(lastEventId))
        {
            var data = await GetDashboardDataAsync();
            return await this.RenderPartialToStringAsync("_Metrics", data);
        }

        return null; // No updates
    });
}

// Or use cached polling for better performance
[HttpGet("/dashboard/metrics/cached")]
public async Task<IActionResult> CachedMetricsPolling()
{
    return await this.CachedPollingFallback(
        cacheKey: "dashboard-metrics",
        getContentFunc: async () => {
            var data = await GetDashboardDataAsync();
            return await this.RenderPartialToStringAsync("_Metrics", data);
        },
        cacheDuration: TimeSpan.FromSeconds(30)
    );
}

// JSON polling for JavaScript-based updates
[HttpGet("/api/notifications/poll")]
public async Task<IActionResult> NotificationsPolling()
{
    return await this.JsonPollingFallback(async lastEventId => {
        var notifications = await GetNotificationsSince(lastEventId);
        return notifications.Any() ? new { notifications } : null;
    });
}
```

## Broadcasting Patterns

### 1. Room-Based Broadcasting

```csharp
// Broadcast to specific rooms
await _connectionRegistry.BroadcastToRoomsAsync(
    eventName: "notification",
    html: notificationHtml,
    rooms: new[] { "admin", "dashboard", "alerts" }
);

// Broadcast to all connections
await _connectionRegistry.BroadcastAsync("global-alert", alertHtml);
```

### 2. User-Based Broadcasting

```csharp
// Broadcast to specific user
await _connectionRegistry.BroadcastToUserAsync(
    eventName: "private-message",
    html: messageHtml,
    userId: "user123"
);

// Broadcast to users with specific roles
await _connectionRegistry.BroadcastToRolesAsync(
    eventName: "admin-notification",
    html: notificationHtml,
    roles: new[] { "Admin", "Manager" }
);
```

### 3. Filtered Broadcasting

```csharp
// Broadcast with custom filter
await _connectionRegistry.BroadcastToFilteredAsync(
    eventName: "department-update",
    html: updateHtml,
    filter: connection => {
        var department = connection.User?.FindFirst("department")?.Value;
        return department == "Engineering" || department == "Product";
    }
);

// Broadcast to authenticated users only
await _connectionRegistry.BroadcastToFilteredAsync(
    eventName: "member-update",
    html: updateHtml,
    filter: connection => connection.User?.Identity?.IsAuthenticated == true
);
```

## Client-Side API

### HTML Attributes

```html
<!-- Basic SSE -->
<div hx-sse="connect:/stream" hx-sse-swap="event-name">
  <!-- Enhanced SSE with fallback -->
  <div
    hx-ext="sse-fallback"
    hx-sse="connect:/stream"
    hx-sse-swap="event-name"
    hx-sse-fallback-url="/fallback-endpoint"
    hx-sse-fallback-interval="5000"
    hx-sse-fallback-type="html"
    hx-sse-max-retries="3"
    hx-sse-debug="true"
  ></div>
</div>
```

### JavaScript Events

```javascript
// Connection lifecycle
document.addEventListener("sse:connected", (e) => {
  console.log("SSE connected:", e.detail.elementId);
});

document.addEventListener("sse:error", (e) => {
  console.log("SSE error:", e.detail.error);
});

document.addEventListener("sse:fallback", (e) => {
  console.log("Switched to polling fallback");
});

// Data events
document.addEventListener("sse:data", (e) => {
  console.log("Received data:", e.detail.data);
});

document.addEventListener("sse:swapped", (e) => {
  console.log("Content swapped:", e.detail.html);
});

// Polling events
document.addEventListener("sse:polling-start", (e) => {
  console.log("Started polling fallback");
});

document.addEventListener("sse:poll-error", (e) => {
  console.log("Polling error:", e.detail.error);
});
```

### Global Configuration

```javascript
// Configure global SSE fallback settings
window.SseFallback.configure({
  pollInterval: 3000,
  maxRetries: 5,
  enableFallback: true,
  debug: true,
});

// Get connection for an element
const connection = window.SseFallback.getConnection(element);
if (connection) {
  connection.stop(); // Manually stop connection
}
```

## Legacy ServerSentEvents() Method

For backwards compatibility, the original `ServerSentEvents()` method remains available:

```csharp
public IActionResult LegacyStream()
{
    return ServerSentEvents(async (stream, cancellationToken) => {
        while (!cancellationToken.IsCancellationRequested)
        {
            var data = await GetDataAsync();
            var html = await this.RenderPartialToStringAsync("_Item", data);
            await stream.SendEventAsync("update", html);

            await stream.SendKeepAliveAsync();
            await Task.Delay(1000, cancellationToken);
        }
    });
}
```

**Migration Note:** For new applications, use `EnhancedServerSentEventsResult` to access advanced features like automatic fallback, room management, and event filtering.

## Production Deployment

### Performance Optimization

```csharp
// 1. Configure connection limits
builder.Services.Configure<SseOptions>(options => {
    options.MaxConnections = 1000;
    options.ConnectionTimeout = TimeSpan.FromMinutes(5);
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
});

// 2. Use Redis for multi-server deployments
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = "localhost:6379";
});

// 3. Configure fallback for unreliable networks
builder.Services.AddSseFallback(options => {
    options.DefaultPollInterval = 10000;  // 10 seconds for production
    options.MaxSseRetries = 2;            // Faster fallback
    options.EnableFallback = true;
});
```

### Monitoring & Debugging

```csharp
// 1. Add connection monitoring
public class SseConnectionMonitor
{
    public int ActiveConnections => _connectionRegistry.GetActiveConnectionCount();
    public TimeSpan AverageConnectionDuration => _connectionRegistry.GetAverageConnectionDuration();
    public Dictionary<string, int> RoomConnectionCounts => _connectionRegistry.GetRoomCounts();
}

// 2. Health checks
builder.Services.AddHealthChecks()
    .AddCheck<SseHealthCheck>("sse-connections");

// 3. Logging integration
services.AddLogging(builder => {
    builder.AddFilter("Swap.Htmx.ServerSentEvents", LogLevel.Information);
});
```

### Load Balancing Considerations

```csharp
// For multi-server deployments, use sticky sessions or Redis backplane
app.UseSession(); // Enable sticky sessions

// Or implement Redis-based connection registry
public class RedisConnectionRegistry : ISseConnectionRegistry
{
    // Implementation for distributed connection management
}
```

## Examples

Complete working examples are available in:

- **Framework Test App**: `framework/Swap.Htmx.TestApp/src` - Basic SSE patterns
- **ProjectHub Demo**: `demo/projecthub/` - Enhanced SSE with fallback
- **E2E Tests**: `framework/Swap.Htmx.E2ETests/` - Browser testing patterns

## Troubleshooting

### Common Issues

1. **SSE Not Connecting**

   ```
   Check: Browser developer tools → Network tab
   Solution: Verify endpoint returns `text/event-stream` content type
   ```

2. **Fallback Not Triggering**

   ```
   Check: JavaScript console for SSE errors
   Solution: Ensure hx-sse-fallback-url is correctly configured
   ```

3. **Broadcasting Not Working**

   ```
   Check: User is joined to correct rooms
   Solution: Use connectionBuilder.WithRooms() or verify room assignment
   ```

4. **Performance Issues**
   ```
   Check: Number of active connections
   Solution: Implement connection pooling and room-based broadcasting
   ```

### Debug Mode

Enable detailed logging:

```html
<div hx-sse-debug="true" ...></div>
```

```javascript
window.SseFallback.configure({ debug: true });
```

```csharp
builder.Services.AddSseFallback(options => {
    options.DebugMode = true;
});
```

## Migration from Basic SSE

### From ServerSentEvents() to Enhanced

**Before:**

```csharp
return ServerSentEvents(async (stream, ct) => {
    await stream.SendEventAsync("update", html);
    await stream.SendKeepAliveAsync();
});
```

**After:**

```csharp
return new EnhancedServerSentEventsResult(async (connectionBuilder, ct) => {
    var connection = connectionBuilder.Connection;
    await connection.SendEventAsync("update", html);
    await connectionBuilder.KeepAlive(TimeSpan.FromSeconds(30), ct);
});
```

### Adding Fallback Support

1. **Register services** in `Program.cs`
2. **Add fallback JavaScript** to your layout
3. **Update HTML attributes** to use `hx-ext="sse-fallback"`
4. **Create polling endpoints** using fallback extensions

That's it! Your existing SSE implementation now has automatic fallback support.

## Best Practices

1. **Always provide fallback endpoints** for production reliability
2. **Use room-based broadcasting** instead of broadcasting to all connections
3. **Implement caching** for polling endpoints to reduce server load
4. **Monitor connection counts** and implement limits for resource management
5. **Use authentication** to secure SSE endpoints and enable user-specific features
6. **Test with network failures** to ensure fallback mechanisms work correctly
7. **Configure appropriate heartbeat intervals** based on your proxy/load balancer settings

## SSE Spec Compliance

Swap.Htmx follows the [W3C Server-Sent Events specification](https://html.spec.whatwg.org/multipage/server-sent-events.html):

- ✅ Proper `Content-Type: text/event-stream` header
- ✅ `event:` and `data:` field formatting
- ✅ Double newline message termination
- ✅ Multi-line data support
- ✅ Comment-based keepalives

## License

MIT License - see [LICENSE](../../../LICENSE) file for details.
