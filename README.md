# Swap.Htmx

**Server-driven web apps, made simple.**

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

Build interactive web applications with server-rendered HTML. Keep JavaScript focused on interactions, not state management. No build tools.

**One event. Multiple UI updates. Zero client-side state.**

### The Code

```csharp
// 1. Controller: Return a SwapView
public IActionResult Index() => SwapView(new TaskModel());

// 2. Action: Perform logic, return updates
[HttpPost]
public IActionResult Complete(int id)
{
    _tasks.Complete(id);
    
    // Updates the "task-list" element with the "_TaskList" partial
    return this.SwapResponse()
        .WithView("_TaskList", _tasks.GetAll()) 
        .WithSuccessToast("Task completed!")
        .Build();
}
```

```html
<!-- 3. View (_TaskList.cshtml): Standard Razor, no JS logic -->
<div id="task-list">
    @foreach (var task in Model) {
        <div class="task @(task.IsComplete ? "done" : "")">
             @task.Title
        </div>
    }
</div>
```

---

## Quick Start

### 1. New Project (Recommended)

```bash
dotnet new install Swap.Templates
dotnet new swap-minimal -n MyApp
cd MyApp
dotnet run
```

### 2. Existing Project

```bash
dotnet add package Swap.Htmx
```

```csharp
// Program.cs
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx(); // <-- Add this

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx(); // <-- Add this
app.MapControllers();
app.Run();
```

---

## Core Concepts

### 1. Smart Views (`SwapView`)

Don't worry about "full page vs partial". `SwapView` handles it.

```csharp
// Browser request -> Returns View("Index", model) (Full Page)
// HTMX request    -> Returns PartialView("Index", model) (Content Only)
return SwapView(model);
```

### 2. Smart State (`SwapState`)

Forget hidden inputs. Use a strong-typed state class.

```csharp
public class FilterState : SwapState 
{ 
    public string Search { get; set; }
    public int Page { get; set; } = 1; 
}
```

```html
<!-- Renders all hidden fields automatically -->
<swap-state state="Model.State" />

<!-- Binds automatically in controller -->
<input type="text" name="Search" hx-get="..." hx-include="#filter-state" />
```

### 3. Smart Events (`SwapEvent`)

Decouple your UI updates. One action can trigger multiple independent components to refresh.

```csharp
// Controller just says "Something happened"
return SwapEvent("task.completed", id).Build();

// Handler updates the stats panel
[SwapHandler]
public class StatsHandler : ISwapEventHandler
{
    public Task HandleAsync(...) => builder.AlsoUpdate("stats", "_Stats", model);
}
```

---

## Advanced Features

### Source Generators (No Magic Strings)
Swap automatically generates constants for your Views and Element IDs.

```csharp
// Instead of "stats-panel" and "_Stats"
builder.AlsoUpdate(SwapElements.StatsPanel, SwapViews.Home._Stats, model);
```

### Tamper-Proof State
Encrypt sensitive state values in the browser automatically.

```csharp
public class PaymentState : SwapState
{
    public override bool Protected => true; // Encrypts all properties
    public decimal Amount { get; set; }
}
```

### SwapStories (Component Playground)
Develop and test partials in isolation.

```csharp
[SwapStory("Product Card", "Components")]
public IActionResult Card() => PartialView("_ProductCard", new Product(...));
```

### Realtime (SSE)
Update clients in real-time without complex WebSocket setup.

```csharp
// Broadcast an update to all connected clients
await _publisher.Publish("task.completed", id);
```

---

## Documentation & Demos
- **[Documentation](lib/Swap.Htmx/docs)**
  - [Getting Started](lib/Swap.Htmx/docs/GettingStarted.md)
- **[SwapStateDemo](demo/SwapStateDemo)** - Secure state & form handling
- **[SwapShop](demo/SwapShop)** - Full E-commerce reference app
- **[SwapDashboard](demo/SwapDashboard)** - Complex multi-component state
- **[SwapDebtors](demo/SwapDebtors)** - Minimal API CRUD example
- **[SwapLab](demo/SwapLab)** - Gallery of 15+ UI patterns

## License

MIT — [LICENSE](LICENSE)
