# WebSockets

Real-time bidirectional communication between server and client using WebSockets.

## Overview

Swap.Htmx provides first-class support for WebSockets, allowing you to build real-time, bidirectional communication features. WebSockets are perfect for:

- Chat applications
- Live collaboration tools
- Real-time multiplayer games
- Live cursors/presence indicators
- Interactive dashboards with user input

## Quick Start

### 1. Create a WebSocket Handler

```csharp
using Swap.Htmx.WebSockets;
using System.Collections.Concurrent;

public class ChatWebSocketHandler : SwapWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();

    protected override Task OnConnectedAsync(WebSocketConnection connection)
    {
        _connections.TryAdd(connection.ConnectionId, connection);
        return Task.CompletedTask;
    }

    protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
    {
        // Parse incoming JSON from HTMX ws-send
        var formData = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
        if (formData == null) return;

        var chatMessage = new ChatMessage
        {
            Username = formData["username"],
            Message = formData["message"],
            Timestamp = DateTime.UtcNow
        };

        // Render HTML using Razor
        var html = await this.RenderPartialToStringAsync("_ChatMessage", chatMessage);

        // Broadcast to all connected clients
        var tasks = _connections.Values
            .Where(c => c.IsOpen)
            .Select(c => c.SendAsync(html));

        await Task.WhenAll(tasks);
    }

    protected override Task OnDisconnectedAsync(WebSocketConnection connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        return Task.CompletedTask;
    }
}
```

### 2. Register WebSocket Handler

In `Program.cs`:

```csharp
using Swap.Htmx.WebSockets;

var app = builder.Build();

// Map WebSocket handler to a path
app.MapSwapWebSocket<ChatWebSocketHandler>("/ws/chat");

app.Run();
```

### 3. Create Partial View with Out-of-Band Swap

`_ChatMessage.cshtml`:

```html
@model ChatMessage

<div hx-swap-oob="beforeend:#chat-messages" class="message">
    <div class="header">
        <span class="username">@Model.Username</span>
        <span class="timestamp">@Model.Timestamp.ToString("HH:mm:ss")</span>
    </div>
    <p>@Model.Message</p>
</div>
```

**Important:** WebSocket messages must use `hx-swap-oob` to tell HTMX where to insert the content.

### 4. Client-Side HTML

```html
<div hx-ext="ws" ws-connect="/ws/chat">
    <div id="chat-messages">
        <!-- Messages will be appended here -->
    </div>
    
    <form ws-send>
        <input name="username" placeholder="Your name" value="User" />
        <input name="message" placeholder="Type a message..." />
        <button type="submit">Send</button>
    </form>
</div>
```

### 5. Install HTMX WebSocket Extension

Add to your layout:

```html
<script src="https://cdn.jsdelivr.net/npm/htmx.org@2.0.8/dist/htmx.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/htmx-ext-ws@2.0.1/ws.js"></script>
```

Or via libman.json:

```json
{
  "libraries": [
    {
      "library": "htmx@2.0.8",
      "destination": "wwwroot/lib/htmx",
      "files": ["htmx.min.js"]
    },
    {
      "library": "htmx-ext-ws@2.0.1",
      "destination": "wwwroot/lib/htmx-ext-ws",
      "provider": "jsdelivr",
      "files": ["ws.js"]
    }
  ]
}
```

## API Reference

### SwapWebSocketHandler (Base Class)

Inherit from this class to create custom WebSocket handlers.

#### Lifecycle Methods

##### `OnConnectedAsync(WebSocketConnection connection)`

Called when a new WebSocket connection is established.

**Parameters:**
- `connection` - The WebSocket connection object

**Example:**
```csharp
protected override Task OnConnectedAsync(WebSocketConnection connection)
{
    _activeConnections.Add(connection.ConnectionId, connection);
    Console.WriteLine($"Client {connection.ConnectionId} connected");
    return Task.CompletedTask;
}
```

##### `OnMessageAsync(WebSocketConnection connection, string message)`

Called when a message is received from the client.

**Parameters:**
- `connection` - The WebSocket connection
- `message` - The received message (JSON from `ws-send` forms)

**Example:**
```csharp
protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
{
    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
    var html = await this.RenderPartialToStringAsync("_Update", data);
    await connection.SendAsync(html);
}
```

##### `OnDisconnectedAsync(WebSocketConnection connection)`

Called when a WebSocket connection is closed.

**Parameters:**
- `connection` - The WebSocket connection that was closed

**Example:**
```csharp
protected override Task OnDisconnectedAsync(WebSocketConnection connection)
{
    _activeConnections.Remove(connection.ConnectionId);
    Console.WriteLine($"Client {connection.ConnectionId} disconnected");
    return Task.CompletedTask;
}
```

#### Helper Methods

##### `RenderPartialToStringAsync<TModel>(string viewName, TModel model)`

Renders a Razor partial view to a string for sending via WebSocket.

**Parameters:**
- `viewName` - Name of the partial view (e.g., "_Message")
- `model` - Model to pass to the view

**Returns:** `Task<string>` containing the rendered HTML

**Example:**
```csharp
var html = await this.RenderPartialToStringAsync("_Notification", notification);
await connection.SendAsync(html);
```

### WebSocketConnection

Represents an active WebSocket connection.

#### Properties

- `ConnectionId` - Unique identifier for this connection
- `WebSocket` - The underlying WebSocket instance
- `IsOpen` - Whether the WebSocket is currently open

#### Methods

##### `SendAsync(string message)`

Sends a text message to the client.

**Parameters:**
- `message` - The message to send (typically HTML)

**Example:**
```csharp
await connection.SendAsync("<div>Hello!</div>");
```

##### `CloseAsync(WebSocketCloseStatus status, string description)`

Closes the WebSocket connection gracefully.

**Parameters:**
- `status` - Close status code (default: NormalClosure)
- `description` - Optional description

**Example:**
```csharp
await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down");
```

### Extension Methods

#### `app.MapSwapWebSocket<THandler>(string path)`

Maps a WebSocket handler to a specific path.

**Parameters:**
- `THandler` - Your WebSocket handler class (must inherit from `SwapWebSocketHandler`)
- `path` - The WebSocket endpoint path (e.g., "/ws/chat")

**Example:**
```csharp
app.MapSwapWebSocket<ChatWebSocketHandler>("/ws/chat");
app.MapSwapWebSocket<GameWebSocketHandler>("/ws/game");
```

## Best Practices

### 1. Always Use Out-of-Band Swaps

WebSocket messages **must** include `hx-swap-oob` attribute to work with HTMX.

❌ **Don't:**
```html
<div class="message">Hello</div>
```

✅ **Do:**
```html
<div hx-swap-oob="beforeend:#messages" class="message">Hello</div>
```

### 2. Use Partials for All HTML

❌ **Don't:**
```csharp
await connection.SendAsync($"<div>{message}</div>");
```

✅ **Do:**
```csharp
var html = await this.RenderPartialToStringAsync("_Message", message);
await connection.SendAsync(html);
```

### 3. Track Connections Safely

Use thread-safe collections for managing multiple connections:

```csharp
private static readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
```

### 4. Handle Disconnections Gracefully

```csharp
protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
{
    var tasks = _connections.Values
        .Where(c => c.IsOpen) // Check before sending
        .Select(c => c.SendAsync(html));
    
    await Task.WhenAll(tasks);
}
```

### 5. Parse JSON Safely

HTMX `ws-send` sends form data as JSON:

```csharp
protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
{
    try
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
        if (data == null) return;
        
        // Use data safely
    }
    catch (JsonException)
    {
        // Invalid JSON - ignore or log
    }
}
```

## Common Patterns

### Chat Application

```csharp
public class ChatWebSocketHandler : SwapWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocketConnection> _users = new();

    protected override async Task OnConnectedAsync(WebSocketConnection connection)
    {
        _users.TryAdd(connection.ConnectionId, connection);
        
        // Send join notification to all users
        var joinHtml = await this.RenderPartialToStringAsync("_SystemMessage", 
            new { Message = "A user joined the chat" });
        
        await BroadcastAsync(joinHtml);
    }

    protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
        if (data == null) return;

        var chatMessage = new ChatMessage
        {
            Username = data["username"],
            Message = data["message"],
            Timestamp = DateTime.UtcNow
        };

        var html = await this.RenderPartialToStringAsync("_ChatMessage", chatMessage);
        await BroadcastAsync(html);
    }

    protected override async Task OnDisconnectedAsync(WebSocketConnection connection)
    {
        _users.TryRemove(connection.ConnectionId, out _);
        
        var leaveHtml = await this.RenderPartialToStringAsync("_SystemMessage",
            new { Message = "A user left the chat" });
        
        await BroadcastAsync(leaveHtml);
    }

    private async Task BroadcastAsync(string html)
    {
        var tasks = _users.Values
            .Where(c => c.IsOpen)
            .Select(c => c.SendAsync(html));
        
        await Task.WhenAll(tasks);
    }
}
```

### Presence Indicators

```csharp
public class PresenceWebSocketHandler : SwapWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, UserPresence> _presence = new();

    protected override async Task OnConnectedAsync(WebSocketConnection connection)
    {
        _presence.TryAdd(connection.ConnectionId, new UserPresence 
        { 
            ConnectionId = connection.ConnectionId,
            LastSeen = DateTime.UtcNow 
        });

        await BroadcastPresenceUpdate();
    }

    protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
    {
        // Update last seen
        if (_presence.TryGetValue(connection.ConnectionId, out var presence))
        {
            presence.LastSeen = DateTime.UtcNow;
            await BroadcastPresenceUpdate();
        }
    }

    private async Task BroadcastPresenceUpdate()
    {
        var activeUsers = _presence.Values.Count();
        var html = await this.RenderPartialToStringAsync("_PresenceCount", 
            new { Count = activeUsers });

        var tasks = _presence.Keys
            .Select(id => _presence[id])
            .Where(p => p.Connection?.IsOpen == true)
            .Select(p => p.Connection!.SendAsync(html));

        await Task.WhenAll(tasks);
    }
}
```

### Real-Time Notifications

```csharp
public class NotificationWebSocketHandler : SwapWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocketConnection> _subscribers = new();

    protected override Task OnConnectedAsync(WebSocketConnection connection)
    {
        _subscribers.TryAdd(connection.ConnectionId, connection);
        return Task.CompletedTask;
    }

    protected override Task OnDisconnectedAsync(WebSocketConnection connection)
    {
        _subscribers.TryRemove(connection.ConnectionId, out _);
        return Task.CompletedTask;
    }

    // Called from elsewhere in your application
    public static async Task SendNotificationToAllAsync(Notification notification)
    {
        // This would be injected or accessed via a service
        // For simplicity, shown as static method
        var handler = new NotificationWebSocketHandler();
        var html = await handler.RenderPartialToStringAsync("_Notification", notification);

        var tasks = _subscribers.Values
            .Where(c => c.IsOpen)
            .Select(c => c.SendAsync(html));

        await Task.WhenAll(tasks);
    }
}
```

## WebSockets vs Server-Sent Events

| Feature | WebSockets | SSE |
|---------|-----------|-----|
| **Direction** | Bidirectional | Server → Client only |
| **Protocol** | `ws://` or `wss://` | Standard HTTP |
| **Use Case** | Chat, collaboration, games | Notifications, feeds, updates |
| **Client Send** | Yes, via `ws-send` | No (use regular AJAX) |
| **Server Push** | Yes | Yes |
| **Reconnection** | Built into extension | Built into extension |
| **Complexity** | More complex | Simpler |

**Choose WebSockets when:**
- You need bidirectional communication
- Users need to send data frequently
- Building real-time collaboration features

**Choose SSE when:**
- Only server needs to push updates
- Simpler implementation is preferred
- Standard HTTP infrastructure is required

## Troubleshooting

### Messages Not Appearing

Ensure your partial view includes `hx-swap-oob`:

```html
<!-- This won't work -->
<div>Message</div>

<!-- This will work -->
<div hx-swap-oob="beforeend:#messages">Message</div>
```

### Connection Closing Immediately

Check that your handler doesn't exit early:

```csharp
// Keep connection alive
protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
{
    // Process message but don't exit the handler early
    await ProcessMessage(message);
    // Connection stays open
}
```

### JSON Parse Errors

HTMX `ws-send` sends form data as JSON object:

```json
{
  "username": "Alice",
  "message": "Hello"
}
```

Ensure you parse it correctly:

```csharp
var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
```

## Examples

See the test app for working examples:
- `ChatWebSocketHandler.cs` - Full chat implementation
- `/test/websocket` - Live demo page

## Further Reading

- [HTMX WebSocket Extension](https://htmx.org/extensions/ws/)
- [WebSocket API (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API)
- [HTMX Out-of-Band Swaps](https://htmx.org/attributes/hx-swap-oob/)
