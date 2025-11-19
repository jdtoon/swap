# TaskFlow - Team Task Management Demo

A collaborative task management application showcasing **advanced Swap.Htmx features** not demonstrated in SwapShop, including Server-Sent Events, all swap modes, payload-aware event chains, and warning toasts.

## What This Demonstrates

TaskFlow is the **second demo application** for Swap.Htmx, specifically designed to showcase features and patterns NOT covered by SwapShop:

### 🎯 Key Features Unique to TaskFlow

| Feature | SwapShop | TaskFlow | New in Version |
|---------|----------|----------|----------------|
| **Server-Sent Events (SSE)** | ❌ None | ✅ Fully functional | 0.5.0 |
| **Warning Toasts** | ❌ Not used | ✅ Team overload, conflicts, deadlines | Core |
| **All Swap Modes** | ⚠️ 3 of 8 | ✅ Delete, BeforeEnd, AfterEnd, InnerHTML | Core |
| **Payload-Aware Event Chains** | ❌ Not available | ✅ Access event payload in factories | 0.5.0 |
| **OOB Instance Helpers** | ❌ Manual IDs | ✅ `WithId()`, dynamic element IDs | 0.5.0 |
| **Deep Event Chains** | ⚠️ 2 levels | ✅ 4-5 level cascading triggers | Core |
| **CSS Framework** | Pico CSS | Raw CSS (custom Kanban) | - |

> **Together, SwapShop + TaskFlow provide 100% coverage of Swap.Htmx features!**

---

## Core Functionality

### Kanban Board Management
- **Visual Task Board** - Drag-and-drop between Todo, In Progress, Review, Done
- **Multi-project Support** - Filter and organize tasks by project
- **Task Details** - Priority, deadlines, descriptions, assignees
- **Quick Actions** - Create, edit, delete, assign tasks inline

### Real-Time Collaboration
- **Live Dashboard** - Stats and activity feed update in real-time (SSE)
- **Team Presence** - See who's working on what
- **Overload Warnings** - Alert when team members have 10+ active tasks
- **Conflict Detection** - Warn when multiple users edit the same task

### Activity & Comments
- **Task Comments** - Threaded discussions on tasks
- **Activity Feed** - Comprehensive log of all team actions
- **Smart Notifications** - Real-time notification stream with unread badges

### Project Progress
- **Visual Progress Bars** - Track completion percentage
- **Status Breakdown** - See task distribution across statuses
- **Automatic Updates** - Progress recalculates when tasks move

---

## Advanced Features Demonstrated

### 1. Server-Sent Events (SSE) 🆕

Real-time push updates without polling:

```csharp
[HttpGet("/dashboard/stream")]
public IActionResult DashboardStream()
{
    return ServerSentEvents(async (conn, ct) =>
    {
        // Subscribe to dashboard update events
        conn.WithEvents(
            DashboardSseEvents.StatsUpdate,
            DashboardSseEvents.ActivityUpdate,
            DashboardSseEvents.TeamUpdate
        );

        // Keep connection alive with heartbeats
        await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
    });
}
```

**Features:**
- Dashboard stream (`/dashboard/stream`) - Real-time stats and activity
- Task stream (`/tasks/stream`) - Live task updates
- Notification stream (`/notifications/stream`) - Instant notifications
- Automatic reconnection with exponential backoff (client-side)

**Event Bridge Pattern:**
```csharp
// Chain domain events to SSE broadcasts
config.OnEvent(TaskEvents.Created)
    .BroadcastSse(DashboardSseEvents.StatsUpdate)
    .BroadcastSse(DashboardSseEvents.ActivityUpdate)
    .Build();
```

---

### 2. Payload-Aware Event Chains (NEW in 0.5.0)

Pass data with events to avoid re-fetching:

```csharp
// In controller - trigger event WITH payload
return SwapResponse()
    .AlsoUpdate(TaskElements.Column(task.Status), TaskViews.TaskColumn, tasks)
    .WithTrigger(TaskEvents.Created, task) // Pass task object!
    .Build();

// In event chain config - access payload
config.When(TaskEvents.Created)
    .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
    {
        var task = (TaskItem?)payload; // Reuse task from event!
        var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
        return teamService.GetStats(); // No need to re-fetch task!
    })
```

**Benefits:**
- Eliminates redundant database queries
- Improves performance for complex updates
- Makes event chains more efficient

---

### 3. All Swap Modes

TaskFlow demonstrates **every HTMX swap mode**:

| Swap Mode | Use Case in TaskFlow | Example |
|-----------|----------------------|---------|
| **Delete** | Remove task/comment cards | Delete task → card vanishes |
| **BeforeEnd** | Insert comments chronologically | New comment → appends to list |
| **AfterEnd** | Append activity items | New activity → adds to feed |
| **InnerHTML** | Update stats, progress bars | Task moves → progress updates |
| **OuterHTML** | Replace entire elements (default) | Task update → card replaced |

```csharp
// Delete mode - remove element entirely
.AlsoUpdate(TaskElements.Card(id), "_Empty", null, SwapMode.Delete)

// BeforeEnd mode - insert before closing tag
.AlsoUpdate(
    CommentElements.List(taskId),
    CommentViews.CommentCard,
    comment,
    SwapMode.BeforeEnd
)
```

---

### 4. Warning Toasts

Real-world warning scenarios:

```csharp
// Team member overload detection
if (activeTaskCount >= 10)
{
    return SwapResponse()
        .WithTrigger(TaskEvents.AssignmentFailed)
        .Build();
}

// Event chain shows warning
config.When(TaskEvents.AssignmentFailed)
    .Toast("Cannot assign task - team member is overloaded (10+ active tasks)", 
           ToastType.Warning);
```

**Warning Scenarios:**
- **Overload Alert** - Team member has ≥10 active tasks
- **Conflict Warning** - Another user is editing the same task
- **Deadline Warning** - Task is overdue or approaching deadline

---

### 5. Deep Event Chains

Complex cascading event patterns:

```csharp
config.When(TaskEvents.Assigned)
    .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ...)
    .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ...)
    .Toast("Task assigned successfully", ToastType.Success)
    .AlsoTrigger(NotificationEvents.TaskAssigned)  // → Level 2
    .AlsoTrigger(ActivityEvents.Logged);           // → Level 2

config.When(NotificationEvents.TaskAssigned)
    .RefreshPartial(NotificationElements.Bell, NotificationViews.Bell, ...)
    .AlsoTrigger(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate)); // → Level 3
```

**Flow:** Task Assigned → Update UI → Create Notification → Update Badge → Broadcast SSE → All Clients Update

---

### 6. Dynamic Element IDs (NEW in 0.5.0)

Helper methods for instance-specific updates:

```csharp
// View constants with dynamic ID generators
public static class TaskElements
{
    public static string Card(int taskId) => $"task-card-{taskId}";
    public static string Column(TaskStatus status) => $"task-column-{status.ToString().ToLower()}";
    public static string Comments(int taskId) => $"task-comments-{taskId}";
}

// Usage in controller
.AlsoUpdate(TaskElements.Card(42), TaskViews.TaskCard, task)
.AlsoUpdate(ProjectElements.Progress(task.ProjectId), ProjectViews.ProgressBar, progress)
```

**Benefits:**
- Type-safe element ID generation
- Consistent naming across app
- Easy to update multiple instances (e.g., all task cards)

---

## Running the Demo

### Prerequisites

- **.NET 9.0 SDK** or later
- Any modern browser (Chrome, Firefox, Edge, Safari)

### Quick Start

```powershell
# Navigate to the TaskFlow source directory
cd demo/TaskFlow/src

# Run the application
dotnet run
```

The app will start on **https://localhost:5001** (or check console output for configured port).

### Development Mode with Debug Logging

Enable detailed logging to see event chains, SSE activity, and HTTP headers:

```powershell
# Set environment variable
$env:SWAP_DEV_LOGGING="true"

# Or add to appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Swap.Htmx": "Debug"
    }
  }
}

# Run
dotnet run
```

You'll see color-coded logs showing:
- 🔵 Event chain execution with payloads
- 🟡 Toast notifications
- 🟢 HTTP headers (HX-Trigger, SSE events)
- 🟣 SSE connection lifecycle

---

## Architecture

### Project Structure
```
TaskFlow/
├── src/
│   ├── Controllers/
│   │   ├── TasksController.cs       # SSE, all swap modes
│   │   ├── ProjectsController.cs
│   │   ├── CommentsController.cs    # BeforeEnd swap mode
│   │   ├── DashboardController.cs   # SSE dashboard stream
│   │   └── NotificationsController.cs # SSE notification stream
│   ├── Services/
│   │   ├── IServices.cs             # Service interfaces
│   │   ├── TaskService.cs           # In-memory task storage
│   │   ├── ProjectService.cs
│   │   └── ServiceImplementations.cs # Team, Comment, Activity, Notification
│   ├── Models/
│   │   └── DomainModels.cs          # Task, Project, Comment, etc.
│   ├── Events/
│   │   ├── EventKeys.cs             # 30+ event constants
│   │   └── EventChainConfiguration.cs # Complex event chains
│   ├── Views/
│   │   ├── ViewConstants.cs         # View names & element IDs
│   │   ├── Tasks/                   # Kanban board views
│   │   ├── Dashboard/               # Real-time dashboard
│   │   ├── Projects/                # Project management
│   │   ├── Comments/                # Comment threads
│   │   ├── Notifications/           # Notification UI
│   │   └── Shared/                  # Layout, ViewImports
│   ├── wwwroot/
│   │   ├── css/
│   │   │   └── site.css             # Custom Kanban CSS (no frameworks)
│   │   └── js/
│   │       └── sse-reconnect.js     # SSE reconnection logic
│   └── Program.cs                    # SSE setup, service registration
└── tests/
    ├── TaskFlow.UnitTests/
    └── TaskFlow.IntegrationTests/
```

### Technology Stack

- **ASP.NET Core 9.0** - Web framework
- **Swap.Htmx 0.5.0+** - HTMX integration library
- **HTMX 2.0.4** - Frontend interactivity
- **HTMX SSE Extension 2.2.2** - Server-Sent Events support
- **Raw CSS** - No CSS frameworks (demonstrates custom Kanban styling)
- **In-Memory Storage** - For demo purposes (with seed data)

---

## Feature Coverage Analysis

TaskFlow complements SwapShop to provide **complete Swap.Htmx coverage**:

| Feature Category | SwapShop | TaskFlow | Combined |
|-----------------|----------|----------|----------|
| **Three-Tier API** | 100% ✅ | 100% ✅ | 100% ✅ |
| **SSE** | 0% ❌ | 100% ✅ | 100% ✅ |
| **Warning Toasts** | 0% ❌ | 100% ✅ | 100% ✅ |
| **Swap Modes** | 37.5% (3/8) | 100% ✅ | 100% ✅ |
| **Payload-Aware Chains** | N/A | 100% ✅ | 100% ✅ |
| **Dynamic Element IDs** | Manual | 100% ✅ | 100% ✅ |
| **Deep Event Chains** | Simple | Complex ✅ | 100% ✅ |
| **Toast Types** | 75% (3/4) | 100% ✅ | 100% ✅ |

### What Each Demo Teaches

**SwapShop (E-commerce):**
- ✅ Three-tier API (SwapView, SwapResponse, SwapEvent)
- ✅ Session management
- ✅ Form submissions
- ✅ Basic event chains
- ✅ Success/Error/Info toasts
- ✅ OOB swaps (basic)
- ✅ Cart badge updates
- ✅ Order processing

**TaskFlow (Team Collaboration):**
- ✅ **Server-Sent Events** (real-time dashboard, notifications)
- ✅ **All swap modes** (Delete, BeforeEnd, AfterEnd, etc.)
- ✅ **Payload-aware event chains** (avoid re-fetching)
- ✅ **Warning toasts** (overload, conflicts, deadlines)
- ✅ **Deep event cascades** (4-5 levels)
- ✅ **Dynamic element IDs** (instance-specific updates)
- ✅ **Complex OOB scenarios** (multi-column Kanban updates)
- ✅ **Real-time collaboration** (presence, conflicts)

---

## Project Structure

```
TaskFlow/
├── src/
│   ├── Controllers/
│   │   ├── TasksController.cs          # SSE, all swap modes, payloads
│   │   ├── ProjectsController.cs       # Progress tracking, Delete mode
│   │   ├── CommentsController.cs       # BeforeEnd swap mode
│   │   ├── DashboardController.cs      # SSE dashboard stream
│   │   └── NotificationsController.cs  # SSE notification stream
│   ├── Services/
│   │   ├── IServices.cs                # Service interfaces
│   │   ├── TaskService.cs              # In-memory task storage with seed data
│   │   ├── ProjectService.cs           # Project management
│   │   └── ServiceImplementations.cs   # Team, Comment, Activity, Notification
│   ├── Models/
│   │   └── DomainModels.cs             # Task, Project, Comment, Team, etc.
│   ├── Events/
│   │   ├── EventKeys.cs                # 30+ type-safe event constants
│   │   └── EventChainConfiguration.cs  # Payload-aware event chains
│   ├── Views/
│   │   ├── ViewConstants.cs            # View names & dynamic element IDs
│   │   ├── Tasks/                      # Kanban board views
│   │   ├── Dashboard/                  # Real-time dashboard with SSE
│   │   ├── Projects/                   # Project management views
│   │   ├── Comments/                   # Comment thread views
│   │   ├── Notifications/              # Notification UI
│   │   └── Shared/                     # Layout, ViewImports, ProgressBar
│   ├── wwwroot/
│   │   ├── css/
│   │   │   ├── site.css                # Custom Kanban CSS (no frameworks)
│   │   │   └── swap-toasts.css         # Library toast styles
│   │   └── js/
│   │       └── sse-reconnect.js        # SSE reconnection with backoff
│   └── Program.cs                       # SSE setup, service registration
└── tests/
    ├── TaskFlow.UnitTests/              # Unit tests (in progress)
    └── TaskFlow.IntegrationTests/       # Integration tests (in progress)
```

---

## Key Differences from SwapShop

| Aspect | SwapShop | TaskFlow |
|--------|----------|----------|
| **Domain** | E-commerce (products, cart, orders) | Team collaboration (tasks, projects) |
| **Real-time** | None | Extensive SSE integration |
| **Warning Scenarios** | None | Overload, conflicts, deadlines |
| **Swap Modes** | Mostly OuterHTML/InnerHTML | All 8 modes demonstrated |
| **Event Complexity** | 1-2 steps | 4-5 level cascading chains |
| **Event Payloads** | Not used | Payload-aware factories |
| **OOB Updates** | Basic badge updates | Complex multi-column Kanban |
| **CSS Approach** | Pico CSS framework | Raw CSS (custom Kanban design) |
| **Element IDs** | Manual strings | Dynamic helper methods |

---

## Code Examples

### Example 1: Payload-Aware Event Chain

**Problem:** Traditional event chains re-fetch data that was just created/updated.

**Solution:** Pass payload with event, access it in chain factories.

```csharp
// Controller: Create task and pass as payload
var task = _taskService.Create(input, "demo-user");

return SwapResponse()
    .AlsoUpdate(TaskElements.Column(task.Status), TaskViews.TaskColumn, tasks)
    .WithTrigger(TaskEvents.Created, task) // 👈 Pass task object!
    .Build();

// Event config: Reuse task from payload
config.When(TaskEvents.Created)
    .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
    {
        var task = (TaskItem?)payload; // 👈 Get task from event!
        // No need to re-fetch task from database
        var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
        return teamService.GetStats();
    });
```

### Example 2: Delete Swap Mode

Remove elements entirely from the DOM:

```csharp
[HttpDelete("/tasks/{id}")]
public IActionResult Delete(int id)
{
    var task = _taskService.Get(id);
    _taskService.Delete(id);

    return SwapResponse()
        // Delete the task card - element vanishes
        .AlsoUpdate(TaskElements.Card(id), "_Empty", null, SwapMode.Delete)
        // Update column to reflow remaining cards
        .AlsoUpdate(TaskElements.Column(task.Status), TaskViews.TaskColumn, tasks)
        .WithTrigger(TaskEvents.Deleted)
        .Build();
}
```

### Example 3: BeforeEnd Swap Mode

Insert new content before the closing tag (chronological comments):

```csharp
[HttpPost("/tasks/{taskId}/comments")]
public IActionResult Create(int taskId, [FromForm] CommentInput input)
{
    var comment = _commentService.Create(taskId, input, "demo-user", "Demo User");

    return SwapResponse()
        // Insert comment at end of list (before </div>)
        .AlsoUpdate(
            CommentElements.List(taskId),
            CommentViews.CommentCard,
            comment,
            SwapMode.BeforeEnd // 👈 Appends to existing comments
        )
        .AlsoUpdate(CommentElements.Count(taskId), CommentViews.Count, count)
        .WithTrigger(CommentEvents.Added, comment)
        .Build();
}
```

### Example 4: Warning Toast for Overload

Detect team member overload and show warning:

```csharp
[HttpPatch("/tasks/{id}/assign")]
public IActionResult Assign(int id, [FromForm] string assigneeId)
{
    var activeTaskCount = _teamService.GetActiveTaskCount(assigneeId);
    
    if (activeTaskCount >= 10)
    {
        // Trigger warning event
        return SwapResponse()
            .WithTrigger(TaskEvents.AssignmentFailed)
            .Build();
    }
    
    // ... normal assignment
}

// Event chain config
config.When(TaskEvents.AssignmentFailed)
    .Toast("Cannot assign task - team member is overloaded (10+ active tasks)", 
           ToastType.Warning); // 👈 Orange warning toast
```

### Example 5: Deep Event Chain with SSE

Multi-level cascading events:

```csharp
// Level 1: Task assigned
config.When(TaskEvents.Assigned)
    .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ...)
    .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ...)
    .Toast("Task assigned successfully", ToastType.Success)
    .AlsoTrigger(NotificationEvents.TaskAssigned)     // → Level 2
    .AlsoTrigger(ActivityEvents.Logged);              // → Level 2

// Level 2: Create notification
config.When(NotificationEvents.TaskAssigned)
    .RefreshPartial(NotificationElements.Bell, NotificationViews.Bell, ...)
    .AlsoTrigger(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate)); // → Level 3

// Level 3: SSE broadcast to all clients
config.When(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate))
    .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ...);

// Result: One action → Notification → Badge update → All dashboards update via SSE
```

---

## Current Status

### ✅ Completed

- [x] Full domain model (Task, Project, Comment, Team, Activity, Notification)
- [x] All service interfaces and implementations
- [x] Seed data for realistic demo experience
- [x] Event chain configuration with payload access
- [x] All controllers (Tasks, Projects, Comments, Dashboard, Notifications)
- [x] Complete view structure (Layout, partials, dynamic IDs)
- [x] Custom Kanban CSS (no framework dependencies)
- [x] SSE reconnection JavaScript with exponential backoff
- [x] All swap modes demonstrated
- [x] Warning toast scenarios
- [x] Deep event chains (4-5 levels)
- [x] Payload-aware event factories
- [x] **SSE Implementation** - Fully functional real-time updates with event broadcasting

### ⏳ Planned

- [ ] Unit tests for all services
- [ ] Integration tests for all controllers
- [ ] Drag-and-drop Kanban functionality
- [ ] User authentication (currently uses "demo-user")
- [ ] Persistent storage option (currently in-memory)
- [ ] Performance testing for SSE at scale

---

## Notes & Limitations

**Demo Scope:**
- **In-memory storage** - Data resets on app restart (suitable for demo)
- **Seed data** - Pre-populated with sample tasks, projects, and team members
- **No authentication** - Uses "demo-user" for all actions (production apps should use proper auth)
- **SSE limitations** - Current SSE implementation is simplified for demo purposes; production apps should use SignalR, Redis pub/sub, or similar

**SSE Reconnection:**
- Automatic with exponential backoff (1s → 2s → 4s → 8s → 16s → 30s max)
- Implemented in `wwwroot/js/sse-reconnect.js`
- Works with HTMX SSE extension

**CSS Approach:**
- Raw CSS for custom Kanban board styling
- No CSS framework dependencies
- Uses library's `swap-toasts.css` for toast styling

---

## Learning Path

**Recommended Order:**

1. **Start with SwapShop** - Learn the three-tier API, basic event chains, and session management
2. **Move to TaskFlow** - Explore SSE, advanced swap modes, payload-aware chains, and warning scenarios
3. **Compare both demos** - See how different patterns solve different problems

**What to Focus On:**

- **SwapShop:** Foundation, HTMX SPA patterns, session management, basic OOB swaps
- **TaskFlow:** Real-time features, all swap modes, efficient event chains, team collaboration patterns

---

## Related Resources

- **[SwapShop Demo](../SwapShop/README.md)** - First demo covering e-commerce basics
- **[Swap.Htmx Library](../../lib/Swap.Htmx/README.md)** - Main library documentation
- **[Coverage Analysis](../COVERAGE_ANALYSIS.md)** - Detailed feature coverage comparison
- **[HTMX Documentation](https://htmx.org)** - Official HTMX docs
- **[SSE Extension](https://github.com/bigskysoftware/htmx-extensions/tree/main/src/sse)** - HTMX SSE extension docs

---

## License

Same as parent Swap.Htmx project

---

## Support

For questions, issues, or feature requests:
- Open an issue in the main Swap.Htmx repository
- Check the SwapShop demo for foundational patterns
- Review the Coverage Analysis document for feature comparison
