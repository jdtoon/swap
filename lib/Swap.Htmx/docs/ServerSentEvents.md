# Server-Sent Events (SSE)

Real-time server-to-client updates over HTTP.

> For bi-directional communication, see [WebSockets](WebSockets.md).

---

## Quick Start

### 1. Create SSE Endpoint

```csharp
public class DashboardController : SwapController
{
    [HttpGet("/dashboard/stream")]
    public IActionResult Stream()
    {
        return ServerSentEvents(async (conn, ct) =>
        {
            conn.WithEvents(
                DashboardEvents.StatsUpdate,
                DashboardEvents.ActivityUpdate
            );
            await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
        });
    }
}
```

### 2. Connect from View

```html
<div hx-ext="sse" sse-connect="/dashboard/stream">
    <div id="stats" sse-swap="stats-update">
        <!-- Updated via SSE -->
    </div>
    <div id="activity" sse-swap="activity-update">
        <!-- Updated via SSE -->
    </div>
</div>
```

### 3. Broadcast Updates

```csharp
[HttpPost]
public IActionResult CreateTask(TaskInput input)
{
    var task = _service.Create(input);
    
    return SwapResponse()
        .WithView("_Task", task)
        .BroadcastSse(DashboardEvents.StatsUpdate)  // All clients update!
        .Build();
}
```

---

## Event Configuration

Configure what renders when SSE events broadcast:

```csharp
// Program.cs
builder.Services.AddSwapHtmx(options =>
{
    options.ConfigureEvents(events =>
    {
        events.When(SseEvents.Broadcast(DashboardEvents.StatsUpdate))
            .RefreshPartial("stats", "_Stats", ctx =>
            {
                var stats = ctx.RequestServices.GetRequiredService<IStatsService>();
                return stats.GetCurrent();
            });
    });
});
```

---

## How It Works

```
Browser A          Server           Browser B
    │                 │                 │
    │ GET /stream     │                 │
    ├────────────────>│                 │
    │ SSE connection  │                 │
    │<────────────────│                 │
    │                 │ GET /stream     │
    │                 │<────────────────┤
    │                 │ SSE connection  │
    │                 ├────────────────>│
    │                 │                 │
    │ POST /tasks     │                 │
    ├────────────────>│                 │
    │                 │ BroadcastSse()  │
    │                 │                 │
    │ SSE: stats HTML │                 │
    │<────────────────┤                 │
    │                 │ SSE: stats HTML │
    │                 ├────────────────>│
    │ HTMX swaps      │                 │ HTMX swaps
```

---

## Rooms (Targeted Broadcasting)

Send to specific groups of clients:

```csharp
// Client joins a room
return ServerSentEvents(async (conn, ct) =>
{
    conn.JoinRoom($"project-{projectId}");
    conn.WithEvents(ProjectEvents.TaskUpdated);
    await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
});

// Broadcast to room only
return SwapResponse()
    .WithView("_Task", task)
    .BroadcastSse(ProjectEvents.TaskUpdated, room: $"project-{projectId}")
    .Build();
```

---

## User-Specific Updates

Send to a specific user across all their connections:

```csharp
return SwapResponse()
    .WithView("_Notification", notification)
    .BroadcastSseToUser(userId, NotificationEvents.New)
    .Build();
```

---

## Distributed (Multi-Server)

For web farms, use Redis backplane:

```csharp
builder.Services.AddSwapHtmx(options =>
{
    options.UseRedisBackplane("localhost:6379");
});
```

See [Redis Backplane](RedisBackplane.md) for details.

---

## Best Practices

1. **Use rooms** for scoped updates (projects, teams, etc.)
2. **Keep connections alive** with heartbeats
3. **Handle reconnection** — HTMX does this automatically
4. **Don't overuse** — SSE is for infrequent updates, not high-frequency data

---

## See Also

- [WebSockets](WebSockets.md) — Bi-directional real-time
- [Redis Backplane](RedisBackplane.md) — Multi-server support
- [Event Chains](EventChains.md) — Event-driven updates
