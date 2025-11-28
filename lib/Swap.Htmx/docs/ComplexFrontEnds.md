# Complex Front-End Guide

This guide covers how Swap.Htmx handles complex front-ends with **50+ partials** that all coordinate through events. The architecture scales linearly - adding new UI components doesn't require changes to existing code.

**Demo:** See the [SwapDashboard demo](/demo/SwapDashboard) for a working example with 20+ partials.

---

## The Challenge

A complex page might have dozens of components that need to stay in sync:

```
┌─────────────────────────────────────────────────────────────────┐
│ Page with 20+ Partials                                          │
├─────────┬───────────────────────────────────────────────────────┤
│ Sidebar │ Header (Search, Notifications, User Menu)             │
│ ┌─────┐ ├───────────────────────────────────────────────────────┤
│ │ Nav │ │ Stats Panel (4 stat cards)                            │
│ ├─────┤ ├──────────┬──────────┬──────────┬───────────┬─────────┤
│ │Fltrs│ │ Column 1 │ Column 2 │ Column 3 │ Column 4  │ Activity│
│ ├─────┤ │ ┌──────┐ │ ┌──────┐ │ ┌──────┐ │ ┌──────┐  │ Feed    │
│ │Team │ │ │Card  │ │ │Card  │ │ │Card  │ │ │Card  │  │ ┌─────┐ │
│ └─────┘ │ │Card  │ │ │Card  │ │ │      │ │ │Card  │  │ │Item │ │
│         │ │Card  │ │ │      │ │ │      │ │ │Card  │  │ │Item │ │
│ ┌─────┐ │ └──────┘ │ └──────┘ │ └──────┘ │ └──────┘  │ │Item │ │
│ │Stats│ ├──────────┴──────────┴──────────┴───────────┤ └─────┘ │
│ └─────┘ │ Detail Panel                               │         │
└─────────┴────────────────────────────────────────────┴─────────┘
```

When a user completes a task, what needs to update?

1. The task card (moves to "Done" column)
2. The "Done" column header (count increases)
3. The stats panel (completed count, progress %)
4. The sidebar stats widget
5. The activity feed (new "completed" entry)
6. The notification badge
7. The team workload section
8. The progress bar
9. The overdue widget (if task was overdue)
10. The task counter in the header

**Without orchestration**, you either:
- Reload the entire page (slow, loses state)
- Have the controller know about all 10+ targets (tight coupling)
- Wire up 10 separate `hx-trigger` listeners (complex, fragile)

---

## The Solution: Events + Handlers

### 1. Controller Fires an Event

The controller does business logic, then fires an event. It doesn't know or care what updates:

```csharp
[HttpPost]
public IActionResult CompleteTask(int id)
{
    var task = _tasks.GetById(id);
    _tasks.UpdateStatus(id, TaskStatus.Done);
    
    // Controller just fires the event
    // It doesn't know what will update
    return SwapEvent(new TaskCompletedEvent(task.Id, task.ProjectId, task.Title))
        .WithSuccessToast($"Completed: {task.Title}")
        .Build();
}
```

### 2. Handlers Update Their Partials

Each handler updates ONE piece of the UI. They're completely decoupled from each other:

```csharp
// Handler 1: Updates stats panel
[SwapHandler]
public class StatsHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var stats = _tasks.GetStats(e.ProjectId);
        builder.AlsoUpdate("stats-panel", "_StatsPanel", stats);
        return Task.CompletedTask;
    }
}

// Handler 2: Updates kanban columns
[SwapHandler]
public class KanbanHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var kanban = _tasks.GetKanban(e.ProjectId);
        builder.AlsoUpdate("kanban-todo", "_KanbanColumn", kanban.TodoTasks);
        builder.AlsoUpdate("kanban-done", "_KanbanColumn", kanban.DoneTasks);
        // ... other columns
        return Task.CompletedTask;
    }
}

// Handler 3: Updates activity feed
[SwapHandler]
public class ActivityHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var activities = _activities.GetRecent(10);
        builder.AlsoUpdate("activity-feed", "_ActivityFeed", activities);
        return Task.CompletedTask;
    }
}

// Handler 4: Updates progress bar
[SwapHandler]
public class ProgressHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var progress = _tasks.GetProgress(e.ProjectId);
        builder.AlsoUpdate("progress-bar", "_ProgressBar", progress);
        return Task.CompletedTask;
    }
}

// Handler 5: Updates notification badge
[SwapHandler]
public class NotificationHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var count = _notifications.GetUnreadCount();
        builder.AlsoUpdate("notification-badge", "_NotificationBadge", count);
        return Task.CompletedTask;
    }
}

// ... more handlers for each component that reacts to this event
```

### 3. Single Response, Multiple Updates

The framework collects all handler outputs into ONE HTTP response:

```http
HTTP/1.1 200 OK
HX-Trigger: {"showToast": {"message": "Completed: Fix login bug", "type": "success"}}
Content-Type: text/html

<div id="stats-panel" hx-swap-oob="true">
    <!-- Stats panel HTML -->
</div>

<div id="kanban-todo" hx-swap-oob="true">
    <!-- Todo column HTML -->
</div>

<div id="kanban-done" hx-swap-oob="true">
    <!-- Done column HTML -->
</div>

<div id="activity-feed" hx-swap-oob="true">
    <!-- Activity feed HTML -->
</div>

<div id="progress-bar" hx-swap-oob="true">
    <!-- Progress bar HTML -->
</div>

<div id="notification-badge" hx-swap-oob="true">
    <!-- Badge HTML -->
</div>
```

The browser (HTMX) swaps each element in place. No full page reload, scroll position preserved.

---

## Architecture Diagram

```
User Action (click "Complete")
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│                         CONTROLLER                              │
│              Does business logic, fires event(s)                │
│                                                                 │
│   return SwapEvent(new TaskCompletedEvent(...)).Build();        │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│                         EVENT BUS                               │
│         Finds all handlers for TaskCompletedEvent               │
└─────────────────────────────────────────────────────────────────┘
        │
        ├──→ StatsHandler          → AlsoUpdate("stats-panel")
        ├──→ KanbanHandler         → AlsoUpdate("kanban-*")
        ├──→ ActivityHandler       → AlsoUpdate("activity-feed")
        ├──→ ProgressHandler       → AlsoUpdate("progress-bar")
        ├──→ NotificationHandler   → AlsoUpdate("notification-badge")
        ├──→ TeamHandler           → AlsoUpdate("team-workload")
        ├──→ TaskCounterHandler    → AlsoUpdate("task-counter")
        └──→ OverdueHandler        → AlsoUpdate("overdue-widget")
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│                    RESPONSE BUILDER                             │
│            Collects all OOB swaps into one response             │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SINGLE HTTP RESPONSE                       │
│   Multiple OOB swaps + toast + HX-Trigger                       │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│                          BROWSER                                │
│              HTMX swaps each element in place                   │
│         (no full page reload, maintains scroll, etc.)           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Scaling to 100+ Partials

### Q: Does every partial need a handler?

**No.** Only partials that react to events need handlers. Static partials, or partials that only update on direct interaction, don't need handlers.

### Q: How do you organize 50+ handlers?

By domain/feature:

```
Handlers/
├── Tasks/
│   ├── TaskCardHandler.cs
│   ├── TaskListHandler.cs
│   ├── TaskDetailHandler.cs
│   └── KanbanHandler.cs
├── Dashboard/
│   ├── StatsHandler.cs
│   ├── ProgressHandler.cs
│   └── ActivityFeedHandler.cs
├── Sidebar/
│   ├── ProjectListHandler.cs
│   ├── TeamWorkloadHandler.cs
│   └── QuickActionsHandler.cs
└── Notifications/
    ├── BadgeHandler.cs
    └── DropdownHandler.cs
```

### Q: What about performance with 50+ handlers?

Handlers are only invoked for events they subscribe to. If `TaskCompletedEvent` fires, only handlers implementing `ISwapEventHandler<TaskCompletedEvent>` run. Others don't execute.

**Benchmarks show:**
- Handler dispatch: sub-microsecond
- Actual cost: rendering partials (which you'd pay regardless)
- Network: single HTTP round-trip

### Q: How do partials share state?

Use SwapState to coordinate state across partials:

```csharp
public class DashboardState : SwapState
{
    public int? SelectedProjectId { get; set; }
    public string ViewMode { get; set; } = "board";
    public string StatusFilter { get; set; } = "all";
    public int Page { get; set; } = 1;
}
```

```html
<!-- State lives in the page once -->
<swap-state state="Model.State" />

<!-- Any partial can include it in requests -->
<div hx-get="/tasks/list" hx-include="[data-swap-state]">
    ...
</div>
```

---

## Conditional Updates

### Only Update if Element Exists

Use `AlsoUpdateIfExists` for partials that might not be on the page:

```csharp
[SwapHandler]
public class TaskDetailHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Only updates if the detail panel is currently showing this task
        builder.AlsoUpdateIfExists($"task-detail-{e.TaskId}", "_TaskDetail", task);
        return Task.CompletedTask;
    }
}
```

### Conditional Based on Logic

Use `AlsoUpdateIf` for business logic conditions:

```csharp
[SwapHandler]
public class AdminPanelHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Only update admin panel if current user is admin
        if (_userContext.IsAdmin)
        {
            builder.AlsoUpdate("admin-panel", "_AdminPanel", GetAdminData());
        }
        return Task.CompletedTask;
    }
}
```

---

## Bulk Updates

For updating many similar elements:

```csharp
[SwapHandler]
public class ProductRowsHandler : ISwapEventHandler<PriceChangedEvent>
{
    public Task HandleAsync(PriceChangedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var affectedProducts = _products.GetByCategory(e.CategoryId);
        
        builder.AlsoUpdateMany(
            items: affectedProducts,
            idSelector: p => $"product-{p.Id}",
            viewName: "_ProductRow"
        );
        
        return Task.CompletedTask;
    }
}
```

---

## Type Safety with Source Generators

Avoid magic strings everywhere:

```csharp
// Events
[SwapEventSource]
public partial class AppEvents
{
    public const string TaskCompleted = "task.completed";
    public const string TaskCreated = "task.created";
}
// Generated: AppEvents.Task.Completed, AppEvents.Task.Created

// Element IDs
[SwapElementSource("Views/Dashboard")]
public partial class DashboardElements { }
// Generated: DashboardElements.StatsPanel, DashboardElements.ActivityFeed, etc.

// View paths
[SwapViewSource("Views/Dashboard")]
public partial class DashboardViews { }
// Generated: DashboardViews.Partials.StatsPanel, DashboardViews.Partials.ActivityFeed, etc.
```

Usage in handlers:

```csharp
builder.AlsoUpdate(
    DashboardElements.StatsPanel,           // Generated: "stats-panel"
    DashboardViews.Partials.StatsPanel,     // Generated: "_StatsPanel"
    stats
);
```

---

## Testing Handlers

Each handler is independently testable:

```csharp
[Fact]
public async Task StatsHandler_UpdatesStatsPanel_OnTaskCompleted()
{
    // Arrange
    var mockTasks = new Mock<ITaskService>();
    mockTasks.Setup(t => t.GetStats(1)).Returns(new DashboardStats { TotalTasks = 10 });
    
    var handler = new StatsHandler(mockTasks.Object);
    var builder = new SwapResponseBuilder();
    var evt = new TaskCompletedEvent(1, 1, "Test Task");
    
    // Act
    await handler.HandleAsync(evt, builder, CancellationToken.None);
    
    // Assert
    var response = builder.Build();
    // Verify OOB swap was added for "stats-panel"
}
```

---

## What Swap.Htmx Provides vs Raw HTMX

| Concern | Raw HTMX | With Swap.Htmx |
|---------|----------|----------------|
| **Multi-partial updates** | Manual OOB construction | Handlers add `AlsoUpdate()`, framework assembles |
| **Decoupling** | Controller knows all targets | Handlers own their partials |
| **Adding new partial** | Edit controller + view | Add handler, done |
| **State** | Manual hidden fields | `SwapState` with binding and sync |
| **Type safety** | String IDs everywhere | Source-generated constants |
| **Testing** | Hard to test partials | Each handler is unit testable |

---

## Summary

1. **Events are the coordination primitive** - "Task completed" not "update these 10 divs"
2. **Handlers own their partials** - Each partial has a handler that knows how to update it
3. **Single response** - Framework collects all updates into one HTTP response
4. **Controller stays simple** - Just business logic + fire event
5. **Adding UI is additive** - New handler, no existing code changes
6. **Performance scales** - O(handlers for this event), not O(all handlers)

---

## See Also

- [Event Chains](EventChains.md) - How handlers are discovered and invoked
- [Out-of-Band Swaps](OutOfBandSwaps.md) - How multi-target updates work
- [SwapState](SwapState.md) - Coordinating state across partials
- [SwapDashboard Demo](/demo/SwapDashboard) - Working example with 20+ partials
