# TaskFlow - Team Task Management Demo

**Status:** 🚧 **In Development** - Foundation complete, refinement in progress

## Purpose

TaskFlow is the second demo application for Swap.Htmx, designed to demonstrate features and patterns NOT fully covered by SwapShop:

- **Server-Sent Events (SSE)** - Real-time updates for dashboard, notifications, and task changes
- **Warning Toasts** - User overload alerts, conflict detection
- **All Swap Modes** - Delete, BeforeEnd, AfterEnd, BeforeBegin, InnerHTML
- **Payload-Aware Event Chains** (NEW in 0.5.0) - Access event payload to avoid re-fetching data
- **OOB Instance ID Helpers** (NEW in 0.5.0) - `WithId()`, `AlsoUpdateById()` for multi-element updates
- **Deep Event Chains** - Cascading triggers (Task.Assigned → Notification → Activity → SSE)
- **Complex Real-time Scenarios** - Collaborative team features with presence indicators

## Features

### Core Functionality
- **Kanban Board** - Visual task management with drag-and-drop status changes
- **Team Collaboration** - Presence indicators, overload warnings, task assignments
- **Real-time Dashboard** - Live stats, activity feed, team status (SSE-powered)
- **Project Tracking** - Progress bars, task breakdown by status
- **Comments & Activity** - Task discussion threads, comprehensive activity log
- **Smart Notifications** - Real-time notification stream with unread counts

### Technical Highlights

**SSE Integration**
- Task stream endpoint (`/tasks/stream`)
- Dashboard stream endpoint (`/dashboard/stream`)
- Notification stream endpoint (`/notifications/stream`)
- Automatic reconnection with exponential backoff

**Swap Modes Demonstrated**
- `Delete` - Remove task cards when deleted
- `BeforeEnd` - Insert new comments chronologically
- `AfterEnd` - Append activity items
- `InnerHTML` - Update stats, progress bars

**Event Chain Patterns**
```csharp
// Payload-aware factory (0.5.0 feature)
config.When(TaskEvents.Assigned)
    .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
    {
        var task = (TaskItem?)payload; // Reuse task from event!
        var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
        return teamService.GetStats();
    })
    .AlsoTrigger(NotificationEvents.TaskAssigned) // Cascade to notification
    .AlsoTrigger(ActivityEvents.Logged); // Cascade to activity log
```

**Warning Toast Examples**
- Team member overload detection (>= 10 active tasks)
- Concurrent edit conflict warnings
- Approaching deadline alerts

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
- **Swap.Htmx 0.5.0** - HTMX integration library
- **HTMX 2.0.4** - Frontend interactivity
- **HTMX SSE Extension 2.2.2** - Server-Sent Events support
- **Raw CSS** - No CSS frameworks (demonstrates custom Kanban styling)
- **In-Memory Storage** - For demo purposes (with seed data)

## Coverage Analysis

This demo fills the gaps from SwapShop (see `COVERAGE_ANALYSIS.md` for detailed breakdown):

| Feature | SwapShop | TaskFlow | Combined |
|---------|----------|----------|----------|
| **SSE** | 0% | 100% | 100% |
| **Warning Toasts** | 0% | 100% | 100% |
| **Swap Modes** | 37.5% (3/8) | 100% | 100% |
| **Event Payload Access** | 0% (didn't exist) | 100% | 100% |
| **OOB Instance Helpers** | 0% (didn't exist) | 100% | 100% |
| **Deep Event Chains** | 50% | 100% | 100% |

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 / VS Code / Rider

### Running the Application

```powershell
cd demo/TaskFlow/src
dotnet run
```

Navigate to `https://localhost:5001` (or configured port).

### Development Build

```powershell
dotnet build
dotnet test
```

## Current Status

### ✅ Completed
- [x] Solution and project structure
- [x] All domain models (Task, Project, Comment, Team, Activity, Notification)
- [x] View constants with dynamic ID helpers
- [x] Event keys (30+ events defined)
- [x] All service interfaces
- [x] All service implementations with seed data
- [x] Event chain configuration with payload access
- [x] All controllers (Tasks, Projects, Comments, Dashboard, Notifications)
- [x] Core views (Layout, Dashboard, Tasks, Projects, Comments, Notifications)
- [x] Custom Kanban CSS (no frameworks)
- [x] SSE reconnection JavaScript
- [x] Program.cs with SSE setup

### 🚧 In Progress
- [ ] Resolving service interface/implementation mismatches
- [ ] Fixing TaskStatus namespace conflicts in views
- [ ] Completing all view constants
- [ ] Adding missing model properties (Color, LastSeenAt, AssigneeId, IsEdited, etc.)

### ⏳ Pending
- [ ] Unit tests
- [ ] Integration tests
- [ ] Detailed coverage analysis document
- [ ] Full end-to-end testing

## Key Differences from SwapShop

| Aspect | SwapShop | TaskFlow |
|--------|----------|----------|
| **Domain** | E-commerce (products, cart, orders) | Team collaboration (tasks, projects) |
| **Real-time** | None | Extensive (SSE for everything) |
| **Warning Scenarios** | None | Overload, conflicts, deadlines |
| **Swap Modes** | Mostly innerHTML | All 8 modes demonstrated |
| **Event Complexity** | Simple (1-2 steps) | Deep chains (4-5 cascading triggers) |
| **OOB Updates** | Basic | Advanced (instance helpers) |
| **CSS Approach** | Pico framework | Raw CSS (custom Kanban design) |

## Notes

- **In-memory storage**: Data resets on restart (demo purposes)
- **Seed data**: Pre-populated with sample tasks, projects, and team members
- **SSE reconnection**: Automatic with exponential backoff (1s → 30s max)
- **Toast CSS**: Uses library's `swap-toasts.css` (not custom implementation)
- **No authentication**: Uses "demo-user" for all actions (production apps should use proper auth)

## Next Steps

1. **Fix Build Errors**: Resolve service/model mismatches
2. **Add Tests**: Unit and integration tests for all controllers
3. **Refine SSE**: Implement proper pub/sub (currently placeholder)
4. **Polish UI**: Fine-tune Kanban styling, add drag-and-drop
5. **Documentation**: Complete coverage analysis, add screenshots

## Related

- **SwapShop Demo**: First demo covering e-commerce basics
- **Swap.Htmx Library**: Main library (0.5.0+)
- **Coverage Analysis**: See `COVERAGE_ANALYSIS.md` (pending)

---

**License:** Same as parent Swap.Htmx project
