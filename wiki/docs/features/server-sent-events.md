---
sidebar_position: 5
---

# Server-Sent Events (SSE)

Real-time HTML streaming from server to client using Server-Sent Events.

## Overview

Swap.Htmx provides first-class support for Server-Sent Events, enabling you to stream HTML updates from the server to the client in real-time. SSE is perfect for:

- Live notifications and alerts
- Real-time dashboards and metrics
- Progress updates and status changes
- Activity feeds and live logs
- Chat messages and updates

## Quick Example

**Controller:**

```csharp
using Swap.Htmx;

public class NotificationsController : SwapController
{
    [HttpGet("/notifications/stream")]
    public IActionResult Stream()
    {
        return ServerSentEvents(async (stream, ct) =>
        {
            // Send real-time notifications
            await foreach (var notification in GetNotificationsAsync(ct))
            {
                var html = await this.RenderPartialToStringAsync(
                    "_Notification", notification);
                await stream.SendEventAsync("notification", html);
            }

            // Close connection gracefully
            await stream.SendEventAsync("close", "done");
        });
    }
}
```

**View:**

```html
<div hx-ext="sse" sse-connect="/notifications/stream" sse-close="close">
  <div id="notifications" sse-swap="notification" hx-swap="afterbegin">
    <!-- New notifications appear here -->
  </div>
</div>

<!-- Include HTMX SSE Extension -->
<script src="https://cdn.jsdelivr.net/npm/htmx-ext-sse@2.2.4/dist/sse.min.js"></script>
```

## How It Works

1. **Controller creates SSE endpoint** - `ServerSentEvents()` returns an `IActionResult` configured for SSE
2. **Browser connects** - `sse-connect` attribute establishes connection to the endpoint
3. **Server sends events** - `stream.SendEventAsync()` sends HTML to specific event names
4. **Browser swaps content** - `sse-swap` attribute matches event names and updates DOM
5. **Connection closes** - `sse-close` event gracefully terminates the stream

## Core API

### SwapController Methods

#### `ServerSentEvents()`

Creates an SSE endpoint that streams events to the client.

```csharp
public IActionResult Stream() => ServerSentEvents(async (stream, ct) =>
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

#### `RenderPartialToStringAsync()`

Renders a Razor partial view to a string for sending via SSE.

```csharp
var html = await this.RenderPartialToStringAsync("_TodoItem", todoItem);
await stream.SendEventAsync("todo-added", html);
```

### ServerSentEventStream Methods

#### `SendEventAsync(eventName, html)`

Sends an SSE event with HTML content.

```csharp
await stream.SendEventAsync("notification", "<div class='alert'>New message!</div>");
```

#### `SendKeepAliveAsync()`

Sends a keepalive comment to prevent connection timeout.

```csharp
await stream.SendKeepAliveAsync(); // Send every ~15-30 seconds
```

## Common Patterns

### Live Notifications

Stream notifications as they arrive:

```csharp
public IActionResult NotificationStream()
{
    return ServerSentEvents(async (stream, ct) =>
    {
        await foreach (var notification in _notificationService.Subscribe(ct))
        {
            var html = await this.RenderPartialToStringAsync(
                "_NotificationItem", notification);
            await stream.SendEventAsync("notification", html);
        }
    });
}
```

### Progress Updates

Show real-time progress for long-running operations:

```csharp
public IActionResult ProcessProgress(int jobId)
{
    return ServerSentEvents(async (stream, ct) =>
    {
        var progress = 0;
        while (progress < 100 && !ct.IsCancellationRequested)
        {
            progress = await _jobService.GetProgressAsync(jobId);

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

Stream live activity updates:

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

Handle different event types in the same connection:

```html
<div hx-ext="sse" sse-connect="/dashboard/stream">
  <div id="stats" sse-swap="stats"></div>
  <div id="alerts" sse-swap="alert" hx-swap="afterbegin"></div>
  <div id="logs" sse-swap="log" hx-swap="beforeend"></div>
</div>
```

### Triggering Actions on Events

Trigger HTMX requests when SSE events arrive:

```html
<div hx-ext="sse" sse-connect="/stream">
  <button hx-get="/refresh" hx-trigger="sse:refresh">
    Refresh on Server Event
  </button>
</div>
```

## Best Practices

### 1. Always Use Razor Partials

❌ **Don't concatenate HTML strings:**

```csharp
await stream.SendEventAsync("msg", $"<div>{message}</div>");
```

✅ **Use Razor partials:**

```csharp
await stream.SendEventAsync("msg",
    await this.RenderPartialToStringAsync("_Message", message));
```

### 2. Handle Cancellation

Check cancellation token to avoid unnecessary work:

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

Always send a close event when done:

```csharp
await stream.SendEventAsync("close", "done");
```

And match it in HTML:

```html
<div sse-close="close"></div>
```

### 4. Send Keepalives

Prevent timeouts for long-running streams:

```csharp
await stream.SendKeepAliveAsync(); // Every 15-30 seconds
```

## Installation

The SSE extension is included with HTMX 2.0. Add to your layout:

**Via CDN:**

```html
<script src="https://cdn.jsdelivr.net/npm/htmx.org@2.0.8/dist/htmx.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/htmx-ext-sse@2.2.4/dist/sse.min.js"></script>
```

**Via libman.json:**

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

## Troubleshooting

### Connection Keeps Reconnecting

Make sure you send the close event and have `sse-close` attribute:

```csharp
await stream.SendEventAsync("close", "done");
```

```html
<div sse-close="close"></div>
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

## When to Use SSE

**SSE is ideal for:**

- Real-time dashboards and metrics
- Live notifications and updates
- Activity feeds and status updates
- Progress indicators for long-running tasks
- Chat applications (receive-only)

**Consider alternatives when:**

- You need bidirectional real-time communication
- Client needs to send frequent updates to server
- Very low latency is critical
- Complex real-time collaboration is required

For these cases, consider using WebSockets directly or other real-time technologies.

## Technical Details

Swap.Htmx's SSE implementation follows the [W3C Server-Sent Events specification](https://html.spec.whatwg.org/multipage/server-sent-events.html):

- ✅ Proper `Content-Type: text/event-stream` header
- ✅ `Cache-Control: no-cache` to prevent caching
- ✅ `event:` and `data:` field formatting
- ✅ Double newline message termination
- ✅ Multi-line data support
- ✅ Comment-based keepalives

## See Also

- [HTMX SSE Extension Documentation](https://htmx.org/extensions/sse/)
- [MDN: Server-Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)
- [Toast Notifications](./toast-notifications) - User feedback patterns
- [Event System](./event-system) - Domain event patterns
