# ProjectHub Enhanced SSE Implementation

## Overview

This enhanced version of ProjectHub demonstrates advanced Server-Sent Events (SSE) capabilities using the Swap framework's new SSE system. It showcases real-time communication patterns, automatic fallback mechanisms, and cross-module event broadcasting.

## New Features Added

### 🚀 Enhanced Dashboard Controller (`/dashboard/enhanced`)

**File:** `src/Web/Controllers/EnhancedDashboardController.cs`

A comprehensive demonstration of advanced SSE patterns:

- **Automatic Fallback**: Graceful degradation to HTTP polling when SSE connections fail
- **Connection Management**: Room-based broadcasting with user authentication support
- **Cross-Module Events**: Real-time updates from Projects, Tasks, and Workspaces modules
- **Manual Broadcasting**: Admin-triggered messages to all connected clients
- **Performance Optimization**: Cached polling endpoints for reduced server load

#### Key Endpoints:

1. **`/dashboard/enhanced`** - Main enhanced dashboard view
2. **`/dashboard/enhanced/live`** - Real-time metrics with fallback support
3. **`/dashboard/enhanced/notifications`** - Live notification stream
4. **`/dashboard/enhanced/activity`** - Cross-module activity feed
5. **`/dashboard/enhanced/broadcast`** - Manual message broadcasting

### 📡 SSE Fallback System

**Files:**

- `src/Web/wwwroot/js/sse-fallback.js` - Client-side fallback extension
- Enhanced server-side fallback extensions integrated from framework

**Features:**

- Automatic retry logic with exponential backoff
- Seamless switching to HTTP polling when SSE fails
- Connection monitoring and debugging
- Support for both HTML and JSON response types
- Configurable polling intervals and retry counts

### 🔄 Real-Time Event Handler

**File:** `src/Web/Infrastructure/RealTimeEventHandler.cs`

Demonstrates event-driven SSE broadcasting:

- Listens for domain events from all modules
- Automatically generates appropriate SSE broadcasts
- Supports activity feeds, notifications, and dashboard updates
- Provides foundation for cross-module real-time communication

### ⚙️ Enhanced Configuration

**File:** `src/Web/Program.cs`

Added SSE services and middleware:

```csharp
// Enhanced SSE services
builder.Services.AddSseEventBridge();
builder.Services.AddSseFallback(options => {
    options.DefaultPollInterval = 3000;
    options.MaxSseRetries = 5;
    options.EnableFallback = true;
});

// SSE event middleware
app.UseSseEventBridge();
```

## Usage Examples

### Basic SSE with Fallback

```html
<div
  id="live-data"
  hx-ext="sse-fallback"
  hx-sse="connect:/dashboard/enhanced/live"
  hx-sse-swap="metrics-update"
  hx-sse-fallback-url="/dashboard/enhanced/live"
  hx-sse-fallback-interval="5000"
>
  <!-- Content updated via SSE -->
</div>
```

### Connection Status Monitoring

```javascript
// Listen for connection events
document.addEventListener("sse:connected", (e) => {
  console.log("SSE connected:", e.detail.elementId);
});

document.addEventListener("sse:fallback", (e) => {
  console.log("Fallback to polling:", e.detail.elementId);
});

document.addEventListener("sse:error", (e) => {
  console.log("Connection error:", e.detail.error);
});
```

### Server-Side Broadcasting

```csharp
// Broadcast to all connections in specific rooms
await _connectionRegistry.BroadcastToRoomsAsync(
    eventName: "notification",
    html: notificationHtml,
    rooms: new[] { "notifications", "global" }
);

// Enhanced SSE with room management
return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) => {
    connectionBuilder.WithRooms("dashboard", "metrics")
                    .WithEventPrefix("ui.dashboard");

    // Send data and keep alive
    var connection = connectionBuilder.Connection;
    await connection.SendEventAsync("update", html);
    await connectionBuilder.KeepAlive(TimeSpan.FromSeconds(30), cancellationToken);
});
```

## Architectural Benefits

### 1. **Reliability**

- Automatic fallback ensures real-time features work even with unstable connections
- Exponential backoff prevents server overload during reconnection attempts
- Graceful error handling maintains user experience

### 2. **Performance**

- Cached polling endpoints reduce server load during fallback scenarios
- Efficient connection management with room-based broadcasting
- Differential updates minimize bandwidth usage

### 3. **Scalability**

- Connection registry allows targeted broadcasting to specific user groups
- Event-driven architecture supports loose coupling between modules
- Configurable fallback options adapt to different deployment environments

### 4. **Developer Experience**

- Declarative HTML attributes for SSE configuration
- Comprehensive debugging and monitoring capabilities
- Simple integration with existing HTMX workflows

## Testing the Implementation

1. **Start ProjectHub**: Run the Web project
2. **Navigate to Enhanced Dashboard**: Click "Enhanced SSE Dashboard" from the home page
3. **Monitor Connections**: Open browser DevTools to see SSE connection logs
4. **Test Fallback**: Disable SSE in DevTools Network tab to trigger polling fallback
5. **Test Broadcasting**: Use the manual broadcast form to send messages to all clients
6. **Multiple Clients**: Open multiple browser tabs to see real-time synchronization

## Integration with Existing Modules

The enhanced SSE system is designed to integrate seamlessly with ProjectHub's modular architecture:

- **Workspaces Module**: Real-time workspace creation/updates
- **Projects Module**: Live project status changes and notifications
- **Tasks Module**: Instant task updates and kanban board synchronization

### Adding SSE to Your Module

1. **Define Events**: Add SSE-specific events to your module's contracts
2. **Emit Events**: Call the `RealTimeEventHandler` when domain events occur
3. **Configure Broadcasting**: Set up appropriate room and event filtering
4. **Update Views**: Add SSE fallback attributes to relevant UI components

## Future Enhancements

Potential areas for expansion:

- **Presence System**: Track and display active users
- **Collaborative Editing**: Real-time document collaboration
- **Push Notifications**: Browser notification integration
- **Mobile Support**: WebSocket fallback for mobile browsers
- **Load Balancing**: Redis backplane for multi-server deployments

## Performance Considerations

- **Connection Limits**: Monitor concurrent SSE connections
- **Memory Usage**: Implement connection pruning for idle clients
- **Network Traffic**: Use targeted broadcasting to minimize bandwidth
- **Fallback Frequency**: Balance responsiveness with server load

This enhanced implementation demonstrates the full power of the Swap framework's SSE capabilities while maintaining the simplicity and reliability that makes HTMX-based architectures so appealing.
