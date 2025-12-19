# Realtime Communication with WebSockets

Swap.Htmx provides built-in support for realtime communication using WebSockets (and Server-Sent Events). This allows you to push updates to clients instantly, enabling features like chat, live notifications, and collaborative editing.

## Packages

- Core: `Swap.Htmx`
- Realtime: `Swap.Htmx.Realtime` (required for WebSockets + SSE APIs)

## Overview

The realtime system in Swap.Htmx is built around the concept of **Event Bridging**. You trigger standard Swap events on the server, and the `RealtimeEventBridge` automatically broadcasts them to connected clients.

Key features:
- **Unified Event System**: Use the same `SwapEventBus` for HTTP responses and Realtime broadcasts.
- **HTMX Integration**: Designed to work seamlessly with the HTMX `ws` extension.
- **Connection Management**: Built-in registry for tracking connections, users, and rooms.
- **Targeted Broadcasts**: Send updates to all users, specific rooms, roles, or individual users.

> **Note:** This guide focuses on WebSockets. For Server-Sent Events (SSE) specific details, see the [Server-Sent Events Guide](ServerSentEvents.md).

For how event names map from `HX-Trigger` to realtime (and what the `sse:` prefix actually means), see [Event Naming & Realtime Routing](EventNamingAndRouting.md).

## Setup

### 1. Register Services

In your `Program.cs`, add the realtime services:

```csharp
builder.Services.AddSwapHtmx()
    .AddSseEventBridge(); // Enables both SSE and WebSockets
```

### 2. Configure Middleware

Add the middleware to the pipeline. **Note:** `UseWebSockets` must be called before `UseSwapHtmx`.

```csharp
var app = builder.Build();

// ... other middleware ...

app.UseWebSockets(); // Native ASP.NET Core WebSockets
app.UseSwapHtmx()
   .UseSseEventBridge(); // Swap Realtime middleware
```

### 3. Map the WebSocket Endpoint

Create an endpoint that upgrades the connection to a WebSocket.

```csharp
app.MapGet("/swap/ws", (IRealtimeConnectionRegistry registry) => 
{
    return SwapRealtimeResults.WebSocket(registry, options => {
        // Optional: Auto-join rooms or subscribe to events
        options.AutoSubscribeRooms = new[] { "global" };
        
        options.OnConnected = async (conn) => {
            Console.WriteLine($"Client connected: {conn.Id}");
        };
    });
});
```

## Client-Side Setup

Use the HTMX `ws` extension to connect to your endpoint.

```html
<div hx-ext="ws" ws-connect="/swap/ws">
    
    <!-- This container will receive OOB swaps -->
    <div id="notifications"></div>

    <!-- Sending messages -->
    <form ws-send>
        <input type="hidden" name="event" value="chat.message" />
        <input type="text" name="payload" />
        <button type="submit">Send</button>
    </form>

</div>
```

## Broadcasting Events

To send updates to clients, you configure an **Event Chain** with `.Broadcast()`.

### Configuration

```csharp
public class ChatEventConfiguration : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions options)
    {
        options.When(ChatEvents.Message)
            .Broadcast() // Broadcasts to all connected clients
            .RefreshPartial("chat-messages", "_ChatMessage", swapMode: SwapMode.BeforeEnd);
    }
}
```

### Triggering the Event

You can trigger the event from a Controller, a Minimal API, or even an incoming WebSocket message.

```csharp
// In a Controller
public IActionResult SendMessage(string message)
{
    return this.SwapResponse()
        .WithTrigger(ChatEvents.Message, new { Text = message })
        .Build();
}
```

When this event is triggered, Swap.Htmx will:
1.  Render the `_ChatMessage` partial view using the payload.
2.  Send the rendered HTML to all connected WebSocket clients.
3.  The clients will append the HTML to the `#chat-messages` element.

## Handling Incoming Messages

When a client sends a message via `<form ws-send>`, the default `IRealtimeInputHandler` expects a JSON payload with an `event` property.

```json
{
    "event": "chat.message",
    "payload": { "text": "Hello World" }
}
```

The handler automatically triggers the corresponding Swap event (`chat.message`), which then executes any configured chains (like the broadcast above).

## Advanced Targeting

You can target specific groups of users using the `Broadcast` method variants or by configuring the event chain.

### Rooms

Clients can join rooms:

```csharp
// Server-side (e.g., in OnConnected)
connection.JoinRoom("room-1");
```

Broadcast to a room:

```csharp
options.When(RoomEvents.Update)
    .Broadcast("room:room-1")
    .RefreshPartial(...);
```

### Users and Roles

The `IRealtimeConnectionRegistry` allows you to target specific users or roles programmatically if you need more control than the event chain provides.

```csharp
await registry.BroadcastToUserAsync("notification", html, "user-123");
```
