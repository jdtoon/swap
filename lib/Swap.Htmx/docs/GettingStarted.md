# Getting Started

Build your first Swap.Htmx app in 5 minutes.

## Prerequisites

- .NET 9.0+
- Basic ASP.NET Core MVC knowledge

## Setup

### 1. Create Project

```bash
dotnet new mvc -n MyApp
cd MyApp
dotnet add package Swap.Htmx
```

Optional (realtime):

```bash
dotnet add package Swap.Htmx.Realtime
```

### 2. Configure (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();  // Add this

// Optional: realtime (SSE/WebSockets)
// builder.Services.AddSseEventBridge();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();              // Add this

// Optional: realtime (requires Swap.Htmx.Realtime)
// app.UseSseEventBridge();
app.MapControllers();
app.Run();
```

### 3. Layout (_Layout.cshtml)

Add before `</head>`:

```html
<link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />
<script src="https://unpkg.com/htmx.org@2.0.8"></script>
<script src="~/_content/Swap.Htmx/js/swap.client.js"></script>
```

---

## Your First Controller

```csharp
using Swap.Htmx;

public class TodosController : SwapController
{
    private static List<string> _todos = new() { "Learn HTMX", "Build something" };

    public IActionResult Index() => SwapView(_todos);
}
```

`SwapView()` auto-detects request type:
- **Browser navigation** → Full page with layout
- **HTMX request** → Partial (no layout)

One view, works both ways.

---

## Add Interactivity

**Controller:**

```csharp
[HttpPost]
public IActionResult Add([FromForm] string title)
{
    _todos.Add(title);
    return SwapView("_TodoList", _todos);
}
```

**View (Index.cshtml):**

```html
@model List<string>

<h1>Todos</h1>

<form hx-post="/Todos/Add" hx-target="#todo-list">
    <input name="title" placeholder="New todo..." />
    <button type="submit">Add</button>
</form>

<ul id="todo-list">
    @await Html.PartialAsync("_TodoList", Model)
</ul>
```

**Partial (_TodoList.cshtml):**

```html
@model List<string>
@foreach (var todo in Model)
{
    <li>@todo</li>
}
```

Submit the form → HTMX posts → Server returns partial → List updates. No page reload.

---

## The Magic: Multi-Part Updates

Update multiple elements + show a toast in one response:

```csharp
[HttpPost]
public IActionResult Add([FromForm] string title)
{
    _todos.Add(title);
    
    return SwapResponse()
        .WithView("_TodoList", _todos)                    // Update the list
        .AlsoUpdate("todo-count", "_Count", _todos.Count) // Update the counter
        .WithSuccessToast($"Added: {title}")              // Show toast
        .Build();
}
```

```html
<h1>Todos <span id="todo-count">@Model.Count</span></h1>
```

**One HTTP request. Three UI updates. Zero JavaScript.**

---

## Source Generators (Eliminate Magic Strings)

Swap.Htmx includes source generators that create type-safe constants at compile time — **with zero configuration**.

### Zero Config (v1.0.6+)

No `.csproj` changes needed! The `Swap.Htmx.targets` file automatically includes your views.

### Type-Safe Events

```csharp
// Define events as string constants
[SwapEventSource]
public static partial class TodoEvents
{
    public const string TodoAdded = "todo.added";
    public const string TodoCompleted = "todo.completed";
}

// Generated at build time → TodoEvents.Todo.Added, TodoEvents.Todo.Completed
```

### Auto-Generated Constants

`SwapViews` and `SwapElements` are auto-generated from your `.cshtml` files:

```csharp
// Instead of magic strings:
builder.AlsoUpdate("todo-count", "_Count", count);

// Use generated constants (controller-grouped):
builder.AlsoUpdate(SwapElements.TodoCount, SwapViews.Todos._Count, count);
```

📖 [Full Source Generators Guide](SourceGenerators.md)

---

## Event Handlers (Decoupled Updates)

For larger apps, decouple UI updates from controllers:

```csharp
// Define event with source generator
[SwapEventSource]
public static partial class TodoEvents
{
    public const string TodoAdded = "todo.added";
}

// Handler updates the count (runs automatically)
[SwapHandler]
public class CountHandler : ISwapEventHandler<TodoPayload>
{
    public Task HandleAsync(TodoPayload payload, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("todo-count", "_Count", GetCount());
        return Task.CompletedTask;
    }
}

// Controller just fires the event (use generated type-safe key)
[HttpPost]
public IActionResult Add(string title)
{
    _todos.Add(title);
    return SwapEvent(TodoEvents.Todo.Added, new TodoPayload(title))
        .WithView("_TodoList", _todos)
        .Build();
}
```

Controller doesn't know about the count. Handler doesn't know about the list. Both update.

---

## Next Steps

| Guide | What You'll Learn |
|-------|-------------------|
| [Patterns](Patterns.md) | Copy-paste recipes for common scenarios |
| [SwapState](SwapState.md) | Server-side state management |
| [Events](EventChains.md) | Event-driven UI coordination |
| [Navigation](SwapNavTagHelper.md) | SPA-style navigation |

---

## Alternative Setups

- **[Minimal APIs](MinimalApis.md)** — Use `SwapResults` instead of controllers
- **[Razor Pages](RazorPages.md)** — Extension methods on `PageModel`
- **Without inheritance** — Use `this.SwapView()` / `this.SwapResponse()` on any `Controller`
