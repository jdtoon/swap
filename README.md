# Swap.Htmx

**Server-driven web apps, made simple.**

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

Build interactive web applications with server-rendered HTML. No JavaScript frameworks. No complex state management. No build tools.

**One event. Multiple UI updates. Zero client-side state.**

```csharp
// Controller fires an event
return SwapEvent(TaskEvents.Completed, task)
    .WithSuccessToast("Done!")
    .Build();

// Handlers update the UI (decoupled, testable, DI-supported)
[SwapHandler]
public class TaskListHandler : ISwapEventHandler<TaskEvent>
{
    public void Handle(SwapEventContext<TaskEvent> ctx)
    {
        ctx.Response.AlsoUpdate("task-list", "_TaskList", GetTasks());
    }
}

[SwapHandler]
public class StatsHandler : ISwapEventHandler<TaskEvent>
{
    public void Handle(SwapEventContext<TaskEvent> ctx)
    {
        ctx.Response.AlsoUpdate("stats", "_Stats", GetStats());
    }
}
// One HTTP request → both handlers run → one response updates everything
```

---

## Quick Start

```bash
# Install the template
dotnet new install Swap.Templates

# Create a new project (recommended)
dotnet new swap-modular -n MyApp
cd MyApp/src

# Restore client libraries (htmx, optional SSE extension)
libman restore

# Run it
dotnet run
```

## Packages

- `Swap.Htmx` — core MVC + Swap responses/events/source generators
- `Swap.Htmx.Realtime` — SSE/WebSockets (adds `AddSseEventBridge`, `UseSseEventBridge`, `SwapRealtimeResults`, `SwapRealtimeController`)
- `Swap.Htmx.Realtime.Redis` — Redis backplane for scaling SSE across instances

Open the URL shown in the console (typically `https://localhost:5001`).

Prefer a minimal starting point?

```bash
dotnet new swap-mvc -n MyApp
cd MyApp
libman restore
dotnet run
```

---

## Core Features

| # | Feature | What It Does |
|---|---------|--------------|
| 1 | **SwapController** | Base controller with HTMX-aware helper methods |
| 2 | **SwapView** | Auto-detects HTMX requests, returns partials or full pages |
| 3 | **SwapResponse** | Fluent builder for multi-target updates, toasts, triggers |
| 4 | **SwapState** | Server-side state in hidden fields, strongly-typed binding |
| 5 | **Event Handlers** | Decouple UI updates from controllers |
| 6 | **SwapNavigation** | SPA-style `<swap-nav>` tag helper |
| 7 | **Source Generators** | Type-safe events, views, and element IDs at compile time |

---

## Source Generators

Swap.Htmx includes source generators that eliminate magic strings at compile time:

### Event Keys

```csharp
// Define events as strings
[SwapEventSource]
public static partial class TaskEvents
{
    public const string TaskCompleted = "task.completed";
    public const string TaskCreated = "task.created";
}

// Generated at build time:
// TaskEvents.Task.Completed → EventKey("task.completed")
// TaskEvents.Task.Created   → EventKey("task.created")

// Use type-safe keys
return SwapEvent(TaskEvents.Task.Completed, payload).Build();
```

### View Names & Element IDs (Zero-Config)

The generators automatically scan your `.cshtml` files and create constants — **no configuration needed**:

```csharp
// Auto-generated from Views/**/*.cshtml (grouped by controller folder)
builder.AlsoUpdate(SwapElements.ProductGrid, SwapViews.Products._Grid, products);

// Instead of magic strings:
builder.AlsoUpdate("product-grid", "_Grid", products);
```

As of v1.0.6, `Swap.Htmx.targets` auto-includes your view folders. Just reference the package and build.

See the [modular template](templates/content/Swap.ModularMonolith) for a complete working example.

📖 [Full Source Generators Guide](framework/Swap.Htmx.Generators/README.md)

---

## The Pattern

```html
<!-- Click a button -->
<button hx-post="/tasks/complete/5" hx-swap="none">Done</button>
```

```csharp
// Server processes it
[HttpPost]
public IActionResult Complete(int id)
{
    _tasks.Complete(id);
    return SwapEvent(TaskEvents.Completed, new { id }).Build();
}
```

```html
<!-- Multiple parts of the page update automatically -->
<div id="task-list"><!-- refreshed --></div>
<div id="stats"><!-- refreshed --></div>
<div id="activity"><!-- refreshed --></div>
```

**No JavaScript. No state synchronization. No re-render debugging.**

---

## Documentation

- **[Getting Started](lib/Swap.Htmx/docs/GettingStarted.md)** — Full setup guide
- **[Public API & Compatibility](lib/Swap.Htmx/docs/PublicApiAndCompatibility.md)** — What we consider stable vs experimental
- **[Patterns Cheatsheet](lib/Swap.Htmx/docs/Patterns.md)** — Copy-paste recipes
- **[SwapState](lib/Swap.Htmx/docs/SwapState.md)** — Server-side state management
- **[Events](lib/Swap.Htmx/docs/EventChains.md)** — Event-driven UI updates
- **[All Docs](lib/Swap.Htmx/docs)** — Complete reference

## Demos

| Demo | Shows |
|------|-------|
| [SwapLab](demo/SwapLab) | Feature showcase |
| [SwapShop](demo/SwapShop) | E-commerce patterns |
| [SwapDashboard](demo/SwapDashboard) | Complex multi-component UI |
| [SwapChat](demo/SwapChat) | Real-time with SSE |

---

## Why Server-Driven?

- **Simpler architecture** — State lives on the server, HTML is the API
- **Faster initial load** — No JavaScript bundle to download and parse
- **SEO by default** — Server-rendered HTML, always
- **One language** — C# end-to-end, no context switching
- **Debuggable** — Step through your UI logic in the debugger

---

## Requirements

- .NET 8.0, 9.0, or 10.0
- ASP.NET Core

## License

MIT — [LICENSE](LICENSE)
