# Server-Sent Events (SSE)

Real-time HTML streaming from server to client using Server-Sent Events.

## Overview

Swap.Htmx provides first-class support for Server-Sent Events, allowing you to stream HTML updates from the server to the client in real-time. SSE is perfect for:

- Live notifications
- Real-time dashboards
- Progress updates
- Activity feeds
- Live chat messages

## Quick Start

### 1. Server-Side Controller

```csharp
using Swap.Htmx;

public class NotificationsController : SwapController
{
    [HttpGet("/notifications/stream")]
    public IActionResult Stream()
    {
        return ServerSentEvents(async (stream, ct) =>
        {
            // Send events as they occur
            await stream.SendEventAsync("notification", 
                await this.RenderPartialToStringAsync("_Notification", model));
            
            // Send keepalive to prevent timeout
            await stream.SendKeepAliveAsync();
            
            // Close connection gracefully when done
            await stream.SendEventAsync("close", "done");
        });
    }
}
```

### 2. Client-Side HTML (HTMX 2.0)

```html
<div hx-ext="sse" sse-connect="/notifications/stream" sse-close="close">
    <div sse-swap="notification" hx-swap="afterbegin">
        <!-- New notifications appear here -->
    </div>
</div>
```

### 3. Install HTMX SSE Extension

Add to your layout:

```html
<script src="https://cdn.jsdelivr.net/npm/htmx.org@2.0.8/dist/htmx.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/htmx-ext-sse@2.2.4/dist/sse.min.js"></script>
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
      "library": "htmx-ext-sse@2.2.4",
      "destination": "wwwroot/lib/htmx-ext-sse",
      "provider": "jsdelivr",
      "files": ["dist/sse.min.js"]
    }
  ]
}
```

## API Reference

### SwapController Methods

#### `ServerSentEvents(Func<ServerSentEventStream, CancellationToken, Task> handler)`

Creates an SSE endpoint that executes the provided handler.

**Parameters:**
- `handler` - Async function that receives a `ServerSentEventStream` and `CancellationToken`

**Returns:** `IActionResult` configured for SSE

**Example:**
```csharp
public IActionResult LiveFeed() => ServerSentEvents(async (stream, ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        var data = await GetLatestDataAsync();
        var html = await this.RenderPartialToStringAsync("_Item", data);
        await stream.SendEventAsync("update", html);
        await Task.Delay(1000, ct);
    }
});
```

#### `RenderPartialToStringAsync<TModel>(string viewName, TModel model)`

Renders a Razor partial view to a string for sending via SSE.

**Parameters:**
- `viewName` - Name of the partial view (e.g., "_Notification")
- `model` - Model to pass to the view

**Returns:** `Task<string>` containing the rendered HTML

**Example:**
```csharp
var html = await this.RenderPartialToStringAsync("_TodoItem", todoItem);
await stream.SendEventAsync("todo-added", html);
```

### ServerSentEventStream Methods

#### `SendEventAsync(string eventName, string html)`

Sends an SSE event with HTML content.

**Parameters:**
- `eventName` - Name of the event (matched by `sse-swap` attribute)
- `html` - HTML content to send

#### `SendKeepAliveAsync()`

Sends a keepalive comment to prevent connection timeout.

## Best Practices

### 1. Always Use Partials for HTML

❌ **Don't:**
```csharp
await stream.SendEventAsync("msg", $"<div>{message}</div>");
```

✅ **Do:**
```csharp
await stream.SendEventAsync("msg", 
    await this.RenderPartialToStringAsync("_Message", message));
```

### 2. Handle Cancellation

```csharp
return ServerSentEvents(async (stream, ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        if (ct.IsCancellationRequested) break;
        
        // Your logic here
        await Task.Delay(1000, ct);
    }
});
```

### 3. Close Connections Gracefully

```csharp
// After sending all events
await stream.SendEventAsync("close", "done");
```

Then in HTML, use `sse-close` attribute:
```html
<div hx-ext="sse" sse-connect="/stream" sse-close="close">
```

### 4. Send Keepalives for Long-Running Streams

```csharp
return ServerSentEvents(async (stream, ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        await stream.SendKeepAliveAsync(); // Every ~15-30 seconds
        await Task.Delay(15000, ct);
    }
});
```

## Common Patterns

### Live Notifications

```csharp
public IActionResult NotificationStream()
{
    return ServerSentEvents(async (stream, ct) =>
    {
        await foreach (var notification in GetNotificationsAsync(ct))
        {
            var html = await this.RenderPartialToStringAsync(
                "_Notification", notification);
            await stream.SendEventAsync("notification", html);
        }
        
        await stream.SendEventAsync("close", "done");
    });
}
```

### Progress Updates

```csharp
public IActionResult ProcessProgress(int jobId)
{
    return ServerSentEvents(async (stream, ct) =>
    {
        var progress = 0;
        while (progress < 100 && !ct.IsCancellationRequested)
        {
            progress = await GetJobProgressAsync(jobId);
            
            var html = await this.RenderPartialToStringAsync(
                "_ProgressBar", new { Percentage = progress });
            await stream.SendEventAsync("progress", html);
            
            await Task.Delay(500, ct);
        }
        
        await stream.SendEventAsync("close", "done");
    });
}
```

### Activity Feed

```csharp
public IActionResult ActivityFeed()
{
    return ServerSentEvents(async (stream, ct) =>
    {
        await using var subscription = _activityService.Subscribe();
        
        await foreach (var activity in subscription.ReadAllAsync(ct))
        {
            var html = await this.RenderPartialToStringAsync(
                "_ActivityItem", activity);
            await stream.SendEventAsync("activity", html);
        }
    });
}
```

## Client-Side Patterns

### Multiple Event Types

```html
<div hx-ext="sse" sse-connect="/dashboard/stream">
    <div id="stats" sse-swap="stats"></div>
    <div id="alerts" sse-swap="alert" hx-swap="afterbegin"></div>
    <div id="logs" sse-swap="log" hx-swap="beforeend"></div>
</div>
```

### Triggering Actions on Events

```html
<div hx-ext="sse" sse-connect="/stream">
    <button hx-get="/refresh" hx-trigger="sse:refresh">
        Refresh on Server Event
    </button>
</div>
```

## Troubleshooting

### Connection Keeps Reconnecting

Make sure you send the close event and have `sse-close` attribute:

```csharp
await stream.SendEventAsync("close", "done");
```

```html
<div sse-close="close">
```

### Events Not Appearing

1. Check browser console for errors
2. Verify event names match between `SendEventAsync` and `sse-swap`
3. Ensure HTMX SSE extension is loaded
4. Check that `hx-ext="sse"` is on the parent element

### Connection Timeout

Send keepalives periodically:

```csharp
await stream.SendKeepAliveAsync();
```

## SSE Spec Compliance

Swap.Htmx follows the [W3C Server-Sent Events specification](https://html.spec.whatwg.org/multipage/server-sent-events.html):

- ✅ Proper `Content-Type: text/event-stream` header
- ✅ `event:` and `data:` field formatting
- ✅ Double newline message termination
- ✅ Multi-line data support
- ✅ Comment-based keepalives

## See Also

- [HTMX SSE Extension Docs](https://htmx.org/extensions/sse/)
- [MDN: Server-Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)
- [Getting Started Guide](GETTING-STARTED.md)
