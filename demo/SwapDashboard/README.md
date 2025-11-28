# SwapDashboard - Complex Front-End Demo

This demo showcases how **Swap.Htmx** handles complex front-ends with **20+ partials** that all coordinate through events.

## The Architecture

```
┌────────────────────────────────────────────────────────────────────────────┐
│                              SwapDashboard                                  │
│                                                                            │
│  ┌──────────────┐  ┌─────────────────────────────────────┐  ┌───────────┐ │
│  │   Sidebar    │  │           Main Content               │  │  Activity │ │
│  │              │  │                                      │  │   Feed    │ │
│  │ ┌──────────┐ │  │  ┌──────────────────────────────┐   │  │           │ │
│  │ │ Projects │ │  │  │      Project Header          │   │  │ ┌───────┐ │ │
│  │ │   List   │ │  │  └──────────────────────────────┘   │  │ │ Items │ │ │
│  │ └──────────┘ │  │  ┌──────────────────────────────┐   │  │ └───────┘ │ │
│  │              │  │  │       Stats Panel            │   │  │           │ │
│  │ ┌──────────┐ │  │  │  ┌────┐ ┌────┐ ┌────┐ ┌────┐│   │  │           │ │
│  │ │   Team   │ │  │  │  │Tot │ │Done│ │Prog│ │Over││   │  │           │ │
│  │ │  Panel   │ │  │  │  └────┘ └────┘ └────┘ └────┘│   │  │           │ │
│  │ └──────────┘ │  │  └──────────────────────────────┘   │  │           │ │
│  │              │  │  ┌──────────────────────────────┐   │  │ ┌───────┐ │ │
│  └──────────────┘  │  │       Progress Bar           │   │  │ │Overdue│ │ │
│                    │  └──────────────────────────────┘   │  │ │Widget │ │ │
│  ┌──────────────┐  │  ┌──────────────────────────────┐   │  │ └───────┘ │ │
│  │    Header    │  │  │        Filter Bar            │   │  └───────────┘ │
│  │ ┌────┐┌────┐ │  │  └──────────────────────────────┘   │                │
│  │ │Srch││Notf│ │  │  ┌──────────────────────────────┐   │                │
│  │ └────┘└────┘ │  │  │       Kanban Board           │   │                │
│  └──────────────┘  │  │  ┌────┐ ┌────┐ ┌────┐ ┌────┐ │   │                │
│                    │  │  │Todo│ │Prog│ │Revw│ │Done│ │   │                │
│                    │  │  │    │ │    │ │    │ │    │ │   │                │
│                    │  │  │Card│ │Card│ │Card│ │Card│ │   │                │
│                    │  │  │Card│ │Card│ │    │ │Card│ │   │                │
│                    │  │  └────┘ └────┘ └────┘ └────┘ │   │                │
│                    │  └──────────────────────────────┘   │                │
│                    └─────────────────────────────────────┘                │
└────────────────────────────────────────────────────────────────────────────┘
```

## Partials (20+)

| # | Partial | Description |
|---|---------|-------------|
| 1 | `_Header` | Top navigation bar |
| 2 | `_NotificationBadge` | Notification count badge |
| 3 | `_NotificationList` | Notification dropdown |
| 4 | `_SearchBox` | Global search |
| 5 | `_ProjectList` | Sidebar project list |
| 6 | `_ProjectItem` | Individual project |
| 7 | `_TeamPanel` | Team members sidebar |
| 8 | `_TeamMember` | Individual team member |
| 9 | `_MainContent` | Main content area |
| 10 | `_ProjectHeader` | Selected project header |
| 11 | `_StatsPanel` | Stats cards |
| 12 | `_TaskCounter` | Task count in header |
| 13 | `_ProgressBar` | Project progress bar |
| 14 | `_FilterBar` | Filter controls |
| 15 | `_FilterResultsCount` | Filtered count |
| 16 | `_KanbanBoard` | Kanban board container |
| 17 | `_KanbanColumn` | Individual column (x4) |
| 18 | `_TaskCard` | Task card |
| 19 | `_TaskDetailPanel` | Task detail side panel |
| 20 | `_CommentList` | Comments on task |
| 21 | `_ActivityFeed` | Activity feed sidebar |
| 22 | `_ActivityItem` | Individual activity |
| 23 | `_OverdueWidget` | Overdue tasks alert |
| 24 | `_QuickActions` | Quick action buttons |

## Event Handlers (12)

When a task is completed, ONE event fires and 10+ handlers run independently:

```csharp
// Controller - just fires the event
[HttpPost]
public IActionResult CompleteTask(int id)
{
    _tasks.UpdateStatus(id, TaskStatus.Done);
    return SwapEvent(new TaskEvent(id, projectId, title))
        .WithSuccessToast($"Completed: {title}")
        .Build();
}
```

These handlers EACH update their own partial:

| Handler | Updates | 
|---------|---------|
| `StatsHandler` | `#stats-panel` |
| `ProjectListHandler` | `#project-list` |
| `KanbanHandler` | All 4 kanban columns |
| `ActivityFeedHandler` | `#activity-feed` |
| `TaskDetailHandler` | `#task-detail-{id}` (if open) |
| `TeamWorkloadHandler` | `#team-panel` |
| `ProgressBarHandler` | `#progress-bar` |
| `NotificationBadgeHandler` | `#notification-badge` |
| `OverdueHandler` | `#overdue-widget` |
| `TaskCounterHandler` | `#task-counter` |

**Result:** Single HTTP response with 10+ OOB swaps. Browser updates all elements in place.

## How It Works

```
User clicks "Complete" on a task
        │
        ▼
┌─────────────────────────────────────┐
│ Controller fires TaskEvent          │
│ return SwapEvent(new TaskEvent(...))│
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│ Event Bus finds all handlers        │
│ for TaskEvent (12 handlers)         │
└─────────────────────────────────────┘
        │
        ├──→ StatsHandler.AlsoUpdate("stats-panel", ...)
        ├──→ ProjectListHandler.AlsoUpdate("project-list", ...)
        ├──→ KanbanHandler.AlsoUpdate("kanban-todo", ...)
        ├──→ KanbanHandler.AlsoUpdate("kanban-inprogress", ...)
        ├──→ KanbanHandler.AlsoUpdate("kanban-review", ...)
        ├──→ KanbanHandler.AlsoUpdate("kanban-done", ...)
        ├──→ ActivityFeedHandler.AlsoUpdate("activity-feed", ...)
        ├──→ ProgressBarHandler.AlsoUpdate("progress-bar", ...)
        ├──→ TaskCounterHandler.AlsoUpdate("task-counter", ...)
        ├──→ OverdueHandler.AlsoUpdate("overdue-widget", ...)
        ├──→ NotificationBadgeHandler.AlsoUpdate("notification-badge", ...)
        └──→ TeamWorkloadHandler.AlsoUpdate("team-panel", ...)
        │
        ▼
┌─────────────────────────────────────┐
│ Single HTTP Response                │
│ - Primary content                   │
│ - 12+ OOB swaps                     │
│ - Toast notification                │
│ - HX-Trigger event                  │
└─────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│ Browser (HTMX)                      │
│ Swaps each element in place         │
│ No full page reload                 │
│ Scroll position preserved           │
│ Animations intact                   │
└─────────────────────────────────────┘
```

## Running the Demo

```bash
cd demo/SwapDashboard/src
dotnet run
```

Open https://localhost:5001

## Key Takeaways

1. **The controller doesn't know what updates** - It just fires an event
2. **Handlers are decoupled** - Each handler updates ONE thing
3. **Adding new UI** = Adding a handler, NOT editing the controller
4. **One request, many updates** - All OOB swaps in one response
5. **Type-safe events** - Source-generated event keys
6. **Testable handlers** - Each handler can be unit tested independently

## Why This Matters

Without Swap.Htmx, completing a task would require either:

1. **Full page reload** (slow, loses state)
2. **Controller knows all 12 update targets** (tight coupling)
3. **Client-side JS wires up 12 `hx-trigger` listeners** (complex, fragile)

With Swap.Htmx:

1. **One event, many reactions** - Automatic coordination
2. **Controller stays simple** - Just business logic + event
3. **No client-side coordination** - Server orchestrates everything
4. **Scale indefinitely** - Add handlers, not controller code
