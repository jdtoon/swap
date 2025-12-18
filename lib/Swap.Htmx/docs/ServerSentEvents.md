# Server-Sent Events (SSE)

Real-time server-to-client updates over HTTP.

> For bi-directional communication, see [WebSockets](WebSockets.md).

---

## Quick Start

### 1. Create SSE Endpoint

```csharp
public class DashboardController : SwapRealtimeController
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
        .WithTrigger(DashboardEvents.StatsUpdate)  // Event chain handles both HTTP + realtime
        .Build();
}
```

Enable realtime broadcasting for that event in your event configuration:

```csharp
// Program.cs
builder.Services
    .AddSwapHtmx(events =>
    {
        events.When(DashboardEvents.StatsUpdate)
              .RefreshPartial("stats", "_Stats", ctx =>
              {
                  var stats = ctx.RequestServices.GetRequiredService<IStatsService>();
                  return stats.GetCurrent();
              })
              .Broadcast(); // sends to all connected SSE clients
    })
    .AddSseEventBridge();

// Pipeline
app.UseSwapHtmx();
app.UseSseEventBridge();
```

> `SwapRealtimeController`, `AddSseEventBridge`, and `UseSseEventBridge` come from the `Swap.Htmx.Realtime` package.

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
    │                 │ WithTrigger()   │
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

// Option A (simple): use event name == trigger name and broadcast to one room
// Configure:
// events.When(ProjectEvents.TaskUpdated).Broadcast(target: $"room:project-{projectId}");
// Note: room name must be known at configuration time.

// Option B (dynamic): broadcast directly via the registry
public sealed class TaskController : Controller
{
    private readonly ISseConnectionRegistry _sse;

    public TaskController(ISseConnectionRegistry sse) => _sse = sse;

    [HttpPost]
    public async Task<IActionResult> UpdateTask(TaskInput input)
    {
        var task = _service.Update(input);
        var html = await this.RenderPartialToStringAsync("_Task", task);
        await _sse.BroadcastToRoomsAsync(ProjectEvents.TaskUpdated.Name, html, new[] { $"project-{input.ProjectId}" });
        return Ok();
    }
}
```

---

## User-Specific Updates

Send to a specific user across all their connections:

```csharp
// User targeting is best done via the registry (userId can be dynamic)
await _sse.BroadcastToUserAsync(NotificationEvents.New.Name, html, userId);
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
