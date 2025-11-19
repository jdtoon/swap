# Server-Sent Events (SSE) Guide

Server-Sent Events provide real-time, server-to-client updates over HTTP. Swap.Htmx makes SSE simple with automatic connection management, event routing, and seamless HTMX integration.

## Table of Contents

- [Quick Start](#quick-start)
- [How It Works](#how-it-works)
- [Setup](#setup)
- [Creating SSE Endpoints](#creating-sse-endpoints)
- [Client-Side Setup](#client-side-setup)
- [Broadcasting Events](#broadcasting-events)
- [Event Chain Integration](#event-chain-integration)
- [Advanced Features](#advanced-features)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Quick Start

**1. Add SSE to your controller:**

```csharp
public class DashboardController : SwapController
{
    [HttpGet("/dashboard/stream")]
    public IActionResult DashboardStream()
    {
        return ServerSentEvents(async (conn, ct) =>
        {
            // Subscribe to events this connection should receive
            conn.WithEvents(
                DashboardSseEvents.StatsUpdate,
                DashboardSseEvents.ActivityUpdate
            );

            // Keep connection alive with periodic heartbeats
            await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
        });
    }
}
```

**2. Connect from your view:**

```html
<div class="dashboard" hx-ext="sse" sse-connect="/dashboard/stream">
    <div id="dashboard-stats" sse-swap="stats-update">
        <!-- Content updated via SSE -->
    </div>
    
    <div id="dashboard-activity" sse-swap="activity-update">
        <!-- Content updated via SSE -->
    </div>
</div>
```

**3. Configure event handlers:**

```csharp
builder.Services.AddSwapHtmx(events =>
{
    // When stats-update SSE event is broadcast, render and send new stats
    events.When(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate))
        .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
        {
            var statsService = ctx.RequestServices.GetRequiredService<IStatsService>();
            return statsService.GetStats();
        });
});
```

**4. Trigger updates from anywhere:**

```csharp
public IActionResult CreateTask([FromForm] TaskInput input)
{
    var task = _taskService.Create(input);
    
    return SwapResponse()
        .WithView("TaskCard", task)
        .WithTrigger(TaskEvents.Created, task)
        .BroadcastSse(DashboardSseEvents.StatsUpdate)  // Triggers real-time update!
        .Build();
}
```

Done! All connected clients receive the update in real-time.

## How It Works

```
┌─────────────┐                    ┌──────────────┐                    ┌─────────────┐
│   Browser   │                    │    Server    │                    │   Browser   │
│   (Tab 1)   │                    │              │                    │   (Tab 2)   │
└──────┬──────┘                    └──────┬───────┘                    └──────┬──────┘
       │                                  │                                   │
       │ GET /dashboard/stream            │                                   │
       ├─────────────────────────────────>│                                   │
       │                                  │                                   │
       │ 200 OK (SSE stream)              │                                   │
       │<─────────────────────────────────┤                                   │
       │                                  │                                   │
       │                                  │ GET /dashboard/stream             │
       │                                  │<──────────────────────────────────┤
       │                                  │                                   │
       │                                  │ 200 OK (SSE stream)               │
       │                                  ├──────────────────────────────────>│
       │                                  │                                   │
       │ POST /tasks (create task)        │                                   │
       ├─────────────────────────────────>│                                   │
       │                                  │                                   │
       │                                  │ 1. Handle request                 │
       │                                  │ 2. Create task                    │
       │                                  │ 3. Trigger SSE broadcast          │
       │                                  │ 4. Render partial view            │
       │                                  │ 5. Send to all connections        │
       │                                  │                                   │
       │ SSE: stats-update (HTML)         │                                   │
       │<─────────────────────────────────┤                                   │
       │ (HTMX swaps in new content)      │                                   │
       │                                  │                                   │
       │                                  │ SSE: stats-update (HTML)          │
       │                                  ├──────────────────────────────────>│
       │                                  │      (HTMX swaps in new content)  │
```

## Setup

**1. Add SSE services in Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Swap.Htmx with SSE support
builder.Services.AddSwapHtmx(events =>
{
    // Configure your event chains here
    ConfigureEventChains(events);
});

var app = builder.Build();

// Enable SSE middleware (must be before UseRouting)
app.UseSwapHtmx();

app.UseRouting();
app.MapControllers();
app.Run();
```

**2. Define SSE event constants:**

```csharp
public static class DashboardSseEvents
{
    public const string StatsUpdate = "stats-update";
    public const string ActivityUpdate = "activity-update";
    public const string TeamUpdate = "team-update";
}
```

**3. Define element IDs and view names:**

```csharp
public static class DashboardElements
{
    public const string Stats = "dashboard-stats";
    public const string Activity = "dashboard-activity";
}

public static class DashboardViews
{
    public const string Stats = "~/Views/Dashboard/Stats.cshtml";
    public const string Activity = "~/Views/Dashboard/Activity.cshtml";
}
```

## Creating SSE Endpoints

### Basic Endpoint

```csharp
[HttpGet("/dashboard/stream")]
public IActionResult DashboardStream()
{
    return ServerSentEvents(async (conn, ct) =>
    {
        // Subscribe to specific events
        conn.WithEvents(
            DashboardSseEvents.StatsUpdate,
            DashboardSseEvents.ActivityUpdate
        );

        // Keep connection alive
        await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
    });
}
```

### Room-Based Broadcasting

Use rooms to send updates to specific groups of users:

```csharp
[HttpGet("/project/{projectId}/stream")]
public IActionResult ProjectStream(int projectId)
{
    return ServerSentEvents(async (conn, ct) =>
    {
        // Join a room for this project
        conn.WithRooms($"project-{projectId}");
        
        conn.WithEvents(
            ProjectSseEvents.TaskUpdate,
            ProjectSseEvents.CommentUpdate
        );

        await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
    });
}
```

Then broadcast to the room:

```csharp
return SwapResponse()
    .WithView("TaskCard", task)
    .BroadcastSseToRoom(ProjectSseEvents.TaskUpdate, $"project-{projectId}")
    .Build();
```

### User-Specific Updates

Send updates to a specific user:

```csharp
[HttpGet("/notifications/stream")]
public IActionResult NotificationStream()
{
    return ServerSentEvents(async (conn, ct) =>
    {
        // Associate connection with current user
        var userId = User.Identity.Name;
        conn.WithUserId(userId);
        
        conn.WithEvents(NotificationSseEvents.New);

        await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
    });
}
```

Then target the user:

```csharp
return SwapResponse()
    .WithView("Notification", notification)
    .BroadcastSseToUser(NotificationSseEvents.New, userId)
    .Build();
```

## Client-Side Setup

### Basic Connection

```html
<div hx-ext="sse" sse-connect="/dashboard/stream">
    <div id="dashboard-stats" sse-swap="stats-update">
        @await Html.PartialAsync(DashboardViews.Stats, Model.Stats)
    </div>
</div>
```

**Important:** 
- Add `hx-ext="sse"` to enable the HTMX SSE extension
- `sse-connect` establishes the connection
- `sse-swap` subscribes the element to specific events

### Multiple Elements

Each element can subscribe to different events:

```html
<div class="dashboard" hx-ext="sse" sse-connect="/dashboard/stream">
    <div id="stats" sse-swap="stats-update">
        <!-- Updated when stats-update event fires -->
    </div>
    
    <div id="activity" sse-swap="activity-update">
        <!-- Updated when activity-update event fires -->
    </div>
    
    <div id="team" sse-swap="team-update">
        <!-- Updated when team-update event fires -->
    </div>
</div>
```

### Connection Status Indicator

Show users when SSE is connected:

```html
<h1>Dashboard 
    <small>
        <span id="sse-status" style="color: orange;">Connecting...</span>
    </small>
</h1>

<script>
    document.body.addEventListener('htmx:sseOpen', function(evt) {
        const status = document.getElementById('sse-status');
        status.textContent = 'Live Updates Connected ✓';
        status.style.color = '#10b981';
    });

    document.body.addEventListener('htmx:sseError', function(evt) {
        const status = document.getElementById('sse-status');
        status.textContent = 'Connection Error';
        status.style.color = '#ef4444';
    });

    document.body.addEventListener('htmx:sseClose', function(evt) {
        const status = document.getElementById('sse-status');
        status.textContent = 'Reconnecting...';
        status.style.color = '#f59e0b';
    });
</script>
```

## Broadcasting Events

### From Controllers

```csharp
public IActionResult UpdateStats()
{
    // Broadcast to all connections subscribed to stats-update
    return SwapResponse()
        .WithView("Stats", newStats)
        .BroadcastSse(DashboardSseEvents.StatsUpdate)
        .Build();
}
```

### From Background Services

```csharp
public class MetricsMonitor : BackgroundService
{
    private readonly ISwapEventBus _eventBus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            
            // Trigger SSE broadcast from background service
            _eventBus.Emit(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate));
        }
    }
}
```

### Broadcast to Multiple Events

```csharp
return SwapResponse()
    .WithView("TaskCard", task)
    .BroadcastSse(TaskSseEvents.ListUpdate)
    .BroadcastSse(DashboardSseEvents.StatsUpdate)
    .BroadcastSse(ProjectSseEvents.ProgressUpdate)
    .Build();
```

## Event Chain Integration

Configure SSE event handlers to automatically render and broadcast partial views:

```csharp
builder.Services.AddSwapHtmx(events =>
{
    // SSE event handler - renders partial when broadcast is triggered
    events.When(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate))
        .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
        {
            var statsService = ctx.RequestServices.GetRequiredService<IStatsService>();
            return statsService.GetStats();
        });

    // Domain event -> SSE broadcast chain
    events.OnEvent(TaskEvents.Created)
        .BroadcastSse(DashboardSseEvents.StatsUpdate)
        .BroadcastSse(DashboardSseEvents.ActivityUpdate)
        .Build();
    
    events.OnEvent(TaskEvents.StatusChanged)
        .BroadcastSse(DashboardSseEvents.StatsUpdate)
        .BroadcastSse(TaskSseEvents.ColumnUpdate)
        .Build();
});
```

Now when you trigger a domain event, it automatically broadcasts via SSE:

```csharp
return SwapResponse()
    .WithView("TaskCard", task)
    .WithTrigger(TaskEvents.Created, task)  // Automatically broadcasts SSE!
    .Build();
```

### Separate Events Per Target

For complex UIs like Kanban boards, use separate SSE events for each target to avoid `htmx:oobErrorNoTarget` errors:

```csharp
// Define separate events
public static class TaskSseEvents
{
    public const string TodoColumnUpdate = "task-column-todo-update";
    public const string InProgressColumnUpdate = "task-column-inprogress-update";
    public const string ReviewColumnUpdate = "task-column-review-update";
    public const string DoneColumnUpdate = "task-column-done-update";
}

// Configure separate handlers
events.When(SseEvents.Broadcast(TaskSseEvents.TodoColumnUpdate))
    .RefreshPartial("task-column-todo", "~/Views/Tasks/TaskColumn.cshtml", ctx =>
    {
        var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
        return taskService.GetByStatus(TaskStatus.Todo);
    });

events.When(SseEvents.Broadcast(TaskSseEvents.InProgressColumnUpdate))
    .RefreshPartial("task-column-inprogress", "~/Views/Tasks/TaskColumn.cshtml", ctx =>
    {
        var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
        return taskService.GetByStatus(TaskStatus.InProgress);
    });

// Subscribe to all events in endpoint
conn.WithEvents(
    TaskSseEvents.TodoColumnUpdate,
    TaskSseEvents.InProgressColumnUpdate,
    TaskSseEvents.ReviewColumnUpdate,
    TaskSseEvents.DoneColumnUpdate
);
```

Client view:

```html
<div class="kanban-board" hx-ext="sse" sse-connect="/tasks/stream">
    <div id="task-column-todo" sse-swap="task-column-todo-update">
        <!-- Todo tasks -->
    </div>
    
    <div id="task-column-inprogress" sse-swap="task-column-inprogress-update">
        <!-- In Progress tasks -->
    </div>
    
    <div id="task-column-review" sse-swap="task-column-review-update">
        <!-- Review tasks -->
    </div>
    
    <div id="task-column-done" sse-swap="task-column-done-update">
        <!-- Done tasks -->
    </div>
</div>
```

## Advanced Features

### Custom Event Data

Pass data to SSE event handlers:

```csharp
// In your event chain configuration
events.When(SseEvents.Broadcast(TaskSseEvents.CommentAdded))
    .RefreshPartial("task-comments", "~/Views/Tasks/Comments.cshtml", (ctx, payload) =>
    {
        // Access the payload passed during broadcast
        var taskId = (int)payload;
        var commentService = ctx.RequestServices.GetRequiredService<ICommentService>();
        return commentService.GetByTaskId(taskId);
    });

// Trigger with payload
_eventBus.Emit(SseEvents.Broadcast(TaskSseEvents.CommentAdded), taskId);
```

### Connection Lifecycle

Monitor connection events:

```csharp
public IActionResult Stream()
{
    return ServerSentEvents(async (conn, ct) =>
    {
        _logger.LogInformation("SSE connection opened: {ConnectionId}", conn.Id);
        
        conn.WithEvents(MyEvents.Update);
        
        try
        {
            await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
        }
        finally
        {
            _logger.LogInformation("SSE connection closed: {ConnectionId}", conn.Id);
        }
    });
}
```

### Manual Event Sending

For low-level control, send events directly:

```csharp
public IActionResult Stream()
{
    return ServerSentEvents(async (stream, ct) =>
    {
        // Send initial connection event
        await stream.SendEventAsync("connected", "Welcome!");
        
        // Send periodic updates
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            
            var html = await RenderPartialViewAsync("Stats", GetStats());
            await stream.SendEventAsync("stats-update", html);
        }
    });
}
```

## Best Practices

### 1. Use Full View Paths

Always use full paths for SSE-rendered views to avoid discovery issues:

```csharp
// ✅ Good - full path
public const string Stats = "~/Views/Dashboard/Stats.cshtml";

// ❌ Bad - relative path
public const string Stats = "Stats";
```

### 2. One Event Per Target Element

Avoid sending multiple updates to the same element ID in a single SSE message:

```csharp
// ✅ Good - separate events for separate targets
events.When(SseEvents.Broadcast("column-todo-update"))
    .RefreshPartial("task-column-todo", ...);

events.When(SseEvents.Broadcast("column-done-update"))
    .RefreshPartial("task-column-done", ...);

// ❌ Bad - multiple handlers for same event, different targets
events.When(SseEvents.Broadcast("column-update"))
    .RefreshPartial("task-column-todo", ...);
    
events.When(SseEvents.Broadcast("column-update"))
    .RefreshPartial("task-column-done", ...);
```

### 3. Keep Heartbeat Intervals Reasonable

Balance connection stability with server load:

```csharp
// ✅ Good - 30 seconds
await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);

// ❌ Too frequent - increases server load
await conn.KeepAlive(TimeSpan.FromSeconds(1), ct);

// ❌ Too long - connections may timeout
await conn.KeepAlive(TimeSpan.FromMinutes(10), ct);
```

### 4. Clean Partial Views

SSE-rendered partials should not include layout or wrapper elements:

```cshtml
@* ✅ Good - clean partial *@
@model List<TaskItem>
@{
    Layout = null;
}

@foreach (var task in Model)
{
    <div class="task-card">@task.Title</div>
}

@* ❌ Bad - includes layout *@
@model List<TaskItem>
@{
    Layout = "_Layout";  <!-- Don't use layouts in SSE partials -->
}
```

### 5. Subscribe Only to Needed Events

Only subscribe connections to events they actually need:

```csharp
// ✅ Good - specific events
conn.WithEvents(
    DashboardSseEvents.StatsUpdate,
    DashboardSseEvents.ActivityUpdate
);

// ❌ Bad - subscribing to everything
conn.WithEvents(
    DashboardSseEvents.StatsUpdate,
    DashboardSseEvents.ActivityUpdate,
    TaskSseEvents.ColumnUpdate,
    ProjectSseEvents.Update,
    // ... many more that this page doesn't use
);
```

### 6. Use Rooms for Scoped Broadcasting

Don't broadcast globally when you can scope to specific users/resources:

```csharp
// ✅ Good - room-scoped
conn.WithRooms($"project-{projectId}");
// ... later ...
.BroadcastSseToRoom(eventName, $"project-{projectId}")

// ❌ Bad - global broadcast when unnecessary
.BroadcastSse(eventName)  // Sends to ALL connections
```

## Troubleshooting

### Connection Not Establishing

**Problem:** SSE connection shows "Connecting..." but never connects.

**Solutions:**
- Ensure `app.UseSwapHtmx()` is called before `app.UseRouting()`
- Verify the endpoint route matches `sse-connect` URL
- Check browser DevTools Network tab for 200 response on `/stream` endpoint
- Confirm `hx-ext="sse"` is present on parent element

### htmx:oobErrorNoTarget Error

**Problem:** Browser console shows `htmx:oobErrorNoTarget` errors.

**Solutions:**
- Use separate SSE events for separate target elements (see [Separate Events Per Target](#separate-events-per-target))
- Ensure element IDs in HTML match exactly what's configured in event chains
- Don't wrap SSE-rendered content in extra divs with IDs

**Example:**

```csharp
// ❌ Bad - both handlers render for same event
events.When(SseEvents.Broadcast("update"))
    .RefreshPartial("target-1", ...);
    
events.When(SseEvents.Broadcast("update"))
    .RefreshPartial("target-2", ...);

// ✅ Good - separate events
events.When(SseEvents.Broadcast("update-1"))
    .RefreshPartial("target-1", ...);
    
events.When(SseEvents.Broadcast("update-2"))
    .RefreshPartial("target-2", ...);
```

### Updates Not Appearing

**Problem:** SSE connection works but updates don't appear in UI.

**Solutions:**
- Check that `sse-swap="event-name"` matches the SSE event name exactly
- Verify element ID in HTML matches the target ID in event chain configuration
- Ensure partial view renders without errors (check server logs)
- Confirm the connection is subscribed to the event via `conn.WithEvents(...)`

### View Rendering Errors

**Problem:** SSE events broadcast but views don't render correctly.

**Solutions:**
- Use full paths: `~/Views/Controller/ViewName.cshtml`
- Ensure partial views have `Layout = null`
- Check that the model type matches what the view expects
- Review server logs for rendering exceptions

### Multiple Tabs Not Updating

**Problem:** Only one browser tab receives updates, others don't.

**Solutions:**
- This is normal - each tab needs its own SSE connection
- Ensure each tab opens the page (triggering `sse-connect`)
- Check browser DevTools to confirm multiple connections in Network tab
- Verify broadcasts are not filtered by user/room when they should be global

### Connection Keeps Reconnecting

**Problem:** SSE connection repeatedly disconnects and reconnects.

**Solutions:**
- Ensure `KeepAlive` is awaited and runs continuously
- Check for exceptions in the SSE endpoint action
- Increase heartbeat interval if network is slow
- Review server logs for connection termination reasons

### Performance Issues with Many Connections

**Problem:** Server struggles with many concurrent SSE connections.

**Solutions:**
- Use rooms to scope broadcasts (don't send to connections that don't need updates)
- Only subscribe connections to events they actually use
- Consider increasing heartbeat interval to reduce traffic
- For high-scale production apps (1000+ concurrent connections), consider:
  - Redis pub/sub for distributed SSE
  - SignalR for more advanced real-time features
  - Dedicated real-time service (Pusher, Ably, etc.)

## See Also

- [Event Chains Documentation](EventChains.md) - Configure event-driven updates
- [Events Documentation](Events.md) - Learn about the event system
- [TaskFlow Demo](../../demo/TaskFlow/) - Full working example with SSE

---

**Note:** The SSE implementation in Swap.Htmx is designed for typical web applications with moderate concurrency (dozens to hundreds of concurrent connections). For high-scale production applications with thousands of concurrent connections, consider using Redis pub/sub or similar distributed messaging solutions.
