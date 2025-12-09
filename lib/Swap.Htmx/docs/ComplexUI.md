# Complex UI Coordination

Coordinating multiple components through events and shared state.

**Demo:** [SwapDashboard](/demo/SwapDashboard) — 20+ partials, all coordinated.

---

## The Problem

A dashboard has 15 components that need to stay in sync:

```
┌────────────────────────────────────────────────────┐
│ Header [notifications] [user menu]                 │
├─────────┬──────────────────────────────────────────┤
│ Sidebar │  Stats Panel (4 cards)                   │
│ ┌─────┐ ├──────────────────────────────────────────┤
│ │Fltrs│ │ Data Grid        │ Activity Feed         │
│ ├─────┤ │ ┌──────┐ ┌────┐  │ ┌─────────────────┐   │
│ │Stats│ │ │Card  │ │Card│  │ │ Task completed  │   │
│ └─────┘ │ │Card  │ │    │  │ │ User joined     │   │
│         │ └──────┘ └────┘  │ └─────────────────┘   │
└─────────┴──────────────────┴───────────────────────┘
```

When user completes a task, what updates?
1. Task card (moves to Done)
2. Column header (count)
3. Stats panel (progress %)
4. Sidebar stats
5. Activity feed
6. Notification badge
7. Progress bar
8. ...maybe 5 more things

**Without orchestration:** Controller knows about 10+ targets = tight coupling nightmare.

---

## The Solution: Events + Handlers

### 1. Controller Fires Event (Knows Nothing)

```csharp
[HttpPost]
public IActionResult Complete(int id)
{
    var task = _tasks.Complete(id);
    
    // Controller fires event, doesn't know what updates
    return SwapEvent(TaskEvents.Completed, new TaskPayload(task))
        .WithSuccessToast("Completed!")
        .Build();
}
```

### 2. Handlers Update Their Piece (Decoupled)

```csharp
// Handler 1: Update stats
[SwapHandler(typeof(TaskEvents), nameof(TaskEvents.Completed))]
public class StatsHandler : ISwapEventHandler<TaskPayload>
{
    private readonly IStatsService _stats;
    public StatsHandler(IStatsService stats) => _stats = stats;
    
    public void Handle(SwapEventContext<TaskPayload> ctx)
    {
        var stats = _stats.GetDashboardStats();
        ctx.Response.AlsoUpdate("stats-panel", "_StatsPanel", stats);
    }
}

// Handler 2: Update activity feed
[SwapHandler(typeof(TaskEvents), nameof(TaskEvents.Completed))]
public class ActivityHandler : ISwapEventHandler<TaskPayload>
{
    public void Handle(SwapEventContext<TaskPayload> ctx)
    {
        ctx.Response.AlsoUpdate("activity-feed", "_ActivityFeed", GetRecent());
    }
}

// Handler 3: Update sidebar
[SwapHandler(typeof(TaskEvents), nameof(TaskEvents.Completed))]
public class SidebarHandler : ISwapEventHandler<TaskPayload>
{
    public void Handle(SwapEventContext<TaskPayload> ctx)
    {
        ctx.Response.AlsoUpdate("sidebar-stats", "_SidebarStats", GetStats());
    }
}
```

**Result:** One event → N handlers → One response with all OOB updates.

### 3. Add New Component = Add New Handler

```csharp
// Later: Add notification badge update
// No changes to controller or other handlers!
[SwapHandler(typeof(TaskEvents), nameof(TaskEvents.Completed))]
public class NotificationHandler : ISwapEventHandler<TaskPayload>
{
    public void Handle(SwapEventContext<TaskPayload> ctx)
    {
        ctx.Response.AlsoUpdate("notification-badge", "_Badge", GetUnread());
    }
}
```

---

## Shared State Pattern

For filter/search/pagination coordination:

```html
<!-- Hidden state container -->
<swap-state state="Model.State" />

<!-- All components include state in requests -->
<div class="tabs">
    <button hx-get="/grid?tab=all&page=1" hx-include="#state">All</button>
    <button hx-get="/grid?tab=active&page=1" hx-include="#state">Active</button>
</div>

<input name="search" 
       hx-get="/grid?page=1" 
       hx-include="#state"
       hx-trigger="keyup changed delay:300ms" />

<div id="grid">
    <!-- Grid reads from state via [FromSwapState] -->
</div>
```

See [SwapState](SwapState.md) for full pattern.

---

## Architecture Diagram

```
User Action
    │
    ▼
Controller (fires event, knows nothing about UI)
    │
    ▼
Event Bus (routes to handlers)
    │
    ├──► Handler A → AlsoUpdate("stats", ...)
    ├──► Handler B → AlsoUpdate("activity", ...)
    ├──► Handler C → AlsoUpdate("sidebar", ...)
    └──► Handler D → AlsoUpdate("badge", ...)
    │
    ▼
Single HTTP Response (all OOB swaps combined)
    │
    ▼
HTMX (applies all swaps to page)
```

---

## Conditional Updates

Handlers can decide whether to update:

```csharp
[SwapHandler(typeof(TaskEvents), nameof(TaskEvents.Completed))]
public class OverdueHandler : ISwapEventHandler<TaskPayload>
{
    public void Handle(SwapEventContext<TaskPayload> ctx)
    {
        // Only update overdue panel if task was overdue
        if (ctx.Payload.WasOverdue)
        {
            ctx.Response.AlsoUpdate("overdue-panel", "_OverduePanel", GetOverdue());
        }
    }
}
```

---

## Testing

Handlers are simple to test in isolation:

```csharp
[Fact]
public void StatsHandler_UpdatesStatsPanel()
{
    var stats = new Mock<IStatsService>();
    stats.Setup(s => s.GetDashboardStats()).Returns(new Stats { Total = 100 });
    
    var handler = new StatsHandler(stats.Object);
    var context = CreateTestContext(new TaskPayload { Id = 1 });
    
    handler.Handle(context);
    
    Assert.Contains(context.Response.OobUpdates, 
        u => u.TargetId == "stats-panel");
}
```

---

## Summary

| Without Swap.Htmx | With Swap.Htmx |
|-------------------|----------------|
| Controller knows all 15 targets | Controller fires one event |
| Change one target = change controller | Add handler, nothing else changes |
| Tight coupling | Complete decoupling |
| Hard to test | Each handler testable |
| Doesn't scale | Scales linearly |

---

## See Also

- [Event Chains](EventChains.md) — Full event system docs
- [Out-of-Band Swaps](OutOfBandSwaps.md) — OOB mechanics
- [SwapState](SwapState.md) — Shared state management
- [SwapDashboard Demo](/demo/SwapDashboard) — Working example
